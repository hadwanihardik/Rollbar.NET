﻿[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("UnitTest.Rollbar")]

namespace Rollbar.Serialization.Json
{
    using Newtonsoft.Json.Linq;
    using Rollbar.Diagnostics;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// A utility class aiding in scrubbing Json data fields.
    /// </summary>
    internal static class JsonScrubber
    {

        /// <summary>
        /// Creates the Json object.
        /// </summary>
        /// <param name="jsonData">The json data.</param>
        /// <returns>JObject.</returns>
        public static JObject CreateJsonObject(string jsonData)
        {
            Assumption.AssertNotNullOrWhiteSpace(jsonData, nameof(jsonData));

            JObject json = JObject.Parse(jsonData);
            return json;
        }

        /// <summary>
        /// Gets the child Json property by name.
        /// </summary>
        /// <param name="root">The root.</param>
        /// <param name="childPropertyName">Name of the child property.</param>
        /// <returns>JProperty.</returns>
        public static JProperty GetChildPropertyByName(JContainer root, string childPropertyName)
        {
            foreach(var child in root.Children())
            {
                if (child is JProperty property && property.Name == childPropertyName)
                {
                    return property;
                }
            }

            return null;
        }

        /// <summary>
        /// Scrubs the json fields by their names.
        /// </summary>
        /// <param name="jsonData">The json data.</param>
        /// <param name="scrubFields">The scrub fields.</param>
        /// <param name="scrubMask">The scrub mask.</param>
        /// <returns>System.String.</returns>
        public static string ScrubJsonFieldsByName(string jsonData, IEnumerable<string> scrubFields, string scrubMask)
        {
            if (scrubFields == null || !scrubFields.Any())
            {
                return jsonData;
            }

            JObject json = JObject.Parse(jsonData);

            ScrubJsonFieldsByName(json, scrubFields, scrubMask);

            return json.ToString();
        }

        /// <summary>
        /// Scrubs the json fields by their names.
        /// </summary>
        /// <param name="json">The json.</param>
        /// <param name="scrubFields">The scrub fields.</param>
        /// <param name="scrubMask">The scrub mask.</param>
        public static void ScrubJsonFieldsByName(JToken json, IEnumerable<string> scrubFields, string scrubMask)
        {
            if (json is JProperty property)
            {
                ScrubJsonFieldsByName(property, scrubFields, scrubMask);
                return;
            }

            foreach (var child in json.Children())
            {
                ScrubJsonFieldsByName(child, scrubFields, scrubMask);
            }
        }

        /// <summary>
        /// Scrubs the json fields by their name.
        /// </summary>
        /// <param name="json">The json.</param>
        /// <param name="scrubFields">The scrub fields.</param>
        /// <param name="scrubMask">The scrub mask.</param>
        public static void ScrubJsonFieldsByName(JProperty json, IEnumerable<string> scrubFields, string scrubMask)
        {
            var fields = scrubFields as string[] ?? scrubFields.ToArray();
            if (fields.Contains(json.Name))
            {
                json.Value = scrubMask;
                return;
            }

            if (json.Value is JContainer propertyValue)
            {
                foreach (var child in propertyValue)
                {
                    ScrubJsonFieldsByName(child, fields, scrubMask);
                }
            }
        }

        /// <summary>
        /// Scrubs the json fields by their full names/paths.
        /// </summary>
        /// <param name="jsonData">The json data.</param>
        /// <param name="scrubFieldsPaths">The scrub fields paths.</param>
        /// <param name="scrubMask">The scrub mask.</param>
        /// <returns>System.String.</returns>
        public static string ScrubJsonFieldsByPaths(string jsonData, IEnumerable<string> scrubFieldsPaths, string scrubMask)
        {
            var fieldsPaths = scrubFieldsPaths as string[] ?? scrubFieldsPaths.ToArray();

            if (fieldsPaths.LongLength == 0)
            {
                return jsonData;
            }

            JObject json = JObject.Parse(jsonData);

            foreach (var path in fieldsPaths)
            {
                JsonScrubber.ScrubJsonPath(json, path, scrubMask);
            }

            return json.ToString();
        }

        /// <summary>
        /// Scrubs the json fields by their full names/paths.
        /// </summary>
        /// <param name="jsonData">The json data.</param>
        /// <param name="scrubFieldsPaths">The scrub fields paths.</param>
        /// <param name="scrubMask">The scrub mask.</param>
        public static void ScrubJsonFieldsByPaths(JObject jsonData, IEnumerable<string> scrubFieldsPaths, string scrubMask)
        {
            if (jsonData == null)
            {
                return;
            }

            var fieldsPaths = scrubFieldsPaths as string[] ?? scrubFieldsPaths.ToArray();

            if (fieldsPaths.LongLength == 0)
            {
                return;
            }

            foreach (var path in fieldsPaths)
            {
                JsonScrubber.ScrubJsonPath(jsonData, path, scrubMask);
            }
        }

        /// <summary>
        /// Scrubs the json fields by their full names/paths.
        /// </summary>
        /// <param name="jsonData">The json data.</param>
        /// <param name="scrubFieldsPaths">The scrub fields paths.</param>
        /// <param name="scrubMask">The scrub mask.</param>
        public static void ScrubJsonFieldsByPaths(JProperty jsonData, IEnumerable<string> scrubFieldsPaths, string scrubMask)
        {
            if (jsonData == null)
            {
                return;
            }

            var fieldsPaths = scrubFieldsPaths as string[] ?? scrubFieldsPaths.ToArray();

            if (fieldsPaths.LongLength == 0)
            {
                return;
            }

            foreach (var path in fieldsPaths)
            {
                JsonScrubber.ScrubJsonPath(jsonData, path, scrubMask);
            }
        }

        /// <summary>
        /// Scrubs the json path.
        /// </summary>
        /// <param name="jsonProperty">The json property.</param>
        /// <param name="scrubPath">The scrub path.</param>
        /// <param name="scrubMask">The scrub mask.</param>
        public static void ScrubJsonPath(JProperty jsonProperty, string scrubPath, string scrubMask)
        {
            if (jsonProperty == null)
            {
                return;
            }

            var jProperty = jsonProperty.SelectToken(scrubPath) as JProperty;
            jProperty?.Replace(new JProperty(jProperty.Name, scrubMask));
        }

        /// <summary>
        /// Scrubs the json path.
        /// </summary>
        /// <param name="jsonData">The json data.</param>
        /// <param name="scrubPath">The scrub path.</param>
        /// <param name="scrubMask">The scrub mask.</param>
        public static void ScrubJsonPath(JObject jsonData, string scrubPath, string scrubMask)
        {
            if (jsonData == null)
            {
                return;
            }

            var jProperty = jsonData.SelectToken(scrubPath)?.Parent as JProperty;
            jProperty?.Replace(new JProperty(jProperty.Name, scrubMask));
        }

    }
}
