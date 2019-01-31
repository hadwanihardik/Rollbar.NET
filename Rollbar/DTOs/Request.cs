﻿namespace Rollbar.DTOs
{
    using Rollbar.Diagnostics;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Linq;
    using Rollbar.Common;

#if (NETSTANDARD || NETCOREAPP)
    using Microsoft.AspNetCore.Http;
#endif

#if (NETCOREAPP)
    using Rollbar.AspNetCore;
#endif

#if (NETFX)
    using System.ServiceModel.Channels;
    using System.Web;
#endif

    /// <summary>
    /// Models Rollbar Request DTO.
    /// </summary>
    /// <seealso cref="Rollbar.DTOs.ExtendableDtoBase" />
    public class Request 
        : ExtendableDtoBase
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="Request"/> class.
        /// </summary>
        public Request()
            : base(null)
        {
        }

#if (NETCOREAPP)

        /// <summary>
        /// Initializes a new instance of the <see cref="Request"/> class.
        /// </summary>
        /// <param name="arbitraryKeyValuePairs">The arbitrary key value pairs.</param>
        /// <param name="httpAttributes">The Rollbar HTTP attributes.</param>
        public Request(
            IDictionary<string, object> arbitraryKeyValuePairs
            , RollbarHttpAttributes httpAttributes
            )
            : base(arbitraryKeyValuePairs)
        {
            if (httpAttributes != null)
            {
                this.SnapProperties(httpAttributes);
            }
        }

        private void SnapProperties(RollbarHttpAttributes httpContext)
        {
            Assumption.AssertNotNull(httpContext, nameof(httpContext));

            this.Url = httpContext.Host.Value + httpContext.Path;
            this.QueryString = httpContext.Query.Value;
            this.Params = null;

            this.Headers = new Dictionary<string, string>(httpContext.Headers.Count());
            foreach (var header in httpContext.Headers)
            {
                if (header.Value.Count() == 0)
                    continue;

                this.Headers.Add(header.Key, StringUtility.Combine(header.Value, ", "));
            }

            this.Method = httpContext.Method;
        }

#endif

#if (NETSTANDARD || NETCOREAPP)

        /// <summary>
        /// Initializes a new instance of the <see cref="Request"/> class.
        /// </summary>
        /// <param name="arbitraryKeyValuePairs">The arbitrary key value pairs.</param>
        public Request(
            IDictionary<string, object> arbitraryKeyValuePairs
            )
            : this(arbitraryKeyValuePairs, null as HttpRequest)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Request"/> class.
        /// </summary>
        /// <param name="arbitraryKeyValuePairs">The arbitrary key value pairs.</param>
        /// <param name="httpRequest">The HTTP request.</param>
        public Request(
            IDictionary<string, object> arbitraryKeyValuePairs
            , HttpRequest httpRequest
            )
            : base(arbitraryKeyValuePairs)
        {
            if (httpRequest != null)
            {
                this.SnapProperties(httpRequest);
            }
        }

        private void SnapProperties(HttpRequest httpRequest)
        {
            Assumption.AssertNotNull(httpRequest, nameof(httpRequest));

            this.Url = httpRequest.Host.Value + httpRequest.Path;
            this.QueryString = httpRequest.QueryString.Value;
            this.Params = null;

            this.Headers = new Dictionary<string, string>(httpRequest.Headers.Count());
            foreach (var header in httpRequest.Headers)
            {
                this.Headers.Add(header.Key, StringUtility.Combine(header.Value, ", "));
            }

            this.Method = httpRequest.Method;
        }

#endif

        /// <summary>
        /// Initializes a new instance of the <see cref="Request"/> class.
        /// </summary>
        /// <param name="arbitraryKeyValuePairs">The arbitrary key value pairs.</param>
        /// <param name="rollbarConfig">The rollbar configuration.</param>
        public Request(
            IDictionary<string, object> arbitraryKeyValuePairs
            , IRollbarConfig rollbarConfig
            )
            : this(arbitraryKeyValuePairs, rollbarConfig, null)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Request" /> class.
        /// </summary>
        /// <param name="arbitraryKeyValuePairs">The arbitrary key value pairs.</param>
        /// <param name="rollbarConfig">The rollbar configuration.</param>
        /// <param name="httpRequest">The HTTP request.</param>
        public Request(
            IDictionary<string, object> arbitraryKeyValuePairs
            , IRollbarConfig rollbarConfig
            , HttpRequestMessage httpRequest
            ) 
            : base(arbitraryKeyValuePairs)
        {
            if (httpRequest != null)
            {
                Assumption.AssertNotNull(rollbarConfig, nameof(rollbarConfig));
                this.SnapProperties(httpRequest, rollbarConfig);
            }
        }

        private void SnapProperties(HttpRequestMessage httpRequest, IRollbarConfig rollbarConfig)
        {
            Assumption.AssertNotNull(httpRequest, nameof(httpRequest));

            this.Url = httpRequest.RequestUri?.AbsoluteUri;
            this.QueryString = httpRequest.RequestUri?.Query;
            this.Params = null;

            this.Headers = new Dictionary<string, string>(httpRequest.Headers.Count());
            foreach (var header in httpRequest.Headers)
            {
                this.Headers.Add(header.Key, StringUtility.Combine(header.Value, ", "));
            }

            this.Method = httpRequest.Method.Method;
            switch(this.Method.ToUpperInvariant())
            {
                case "POST":
                    var task = httpRequest.Content.ReadAsStringAsync();
                    task.Wait();
                    this.PostBody = task.Result;
                    this.PostParams = null;
                    break;
                case "GET":
                    this.GetParams = null;
                    break;
                default:
                    System.Diagnostics.Trace.WriteLine(
                        $"No-op processing {this.Method.ToUpperInvariant()} HTTP method."
                        );
                    break;
            }

#if (NETFX)
            string userIP = null;
            const string HttpContextProperty = "MS_HttpContext";
            const string RemoteEndpointMessagePropery = "System.ServiceModel.Channels.RemoteEndpointMessageProperty";
            if (httpRequest.Properties.ContainsKey(HttpContextProperty))
            {
                HttpContextBase ctx = httpRequest.Properties[HttpContextProperty] as HttpContextBase;
                if (ctx != null)
                {
                    userIP = ctx.Request.UserHostAddress;
                }
            }
            else if (httpRequest.Properties.ContainsKey(RemoteEndpointMessagePropery))
            {
                RemoteEndpointMessageProperty remoteEndpoint = 
                    httpRequest.Properties[RemoteEndpointMessagePropery] as RemoteEndpointMessageProperty;
                if (remoteEndpoint != null)
                {
                    userIP = remoteEndpoint.Address;
                }
            }
            this.UserIp = 
                Request.DecideCollectableUserIPValue(userIP, rollbarConfig.IpAddressCollectionPolicy);
#endif
        }

        private static string DecideCollectableUserIPValue(string initialIPAddress, IpAddressCollectionPolicy ipAddressCollectionPolicy)
        {
            switch(ipAddressCollectionPolicy)
            {
                case IpAddressCollectionPolicy.Collect:
                    return initialIPAddress;
                case IpAddressCollectionPolicy.CollectAnonymized:
                    return IpAddressUtility.Anonymize(initialIPAddress);
                case IpAddressCollectionPolicy.DoNotCollect:
                    return null;
                default:
                    Assumption.FailValidation(
                        "Unexpected enumeration value!"
                        , nameof(ipAddressCollectionPolicy)
                        );
                    break;
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        public static class ReservedProperties
        {
            /// <summary>
            /// The URL
            /// </summary>
            public static readonly string Url = "url";
            /// <summary>
            /// The method
            /// </summary>
            public static readonly string Method = "method";
            /// <summary>
            /// The headers
            /// </summary>
            public static readonly string Headers = "headers";
            /// <summary>
            /// The parameters
            /// </summary>
            public static readonly string Params = "params";
            /// <summary>
            /// The get-parameters
            /// </summary>
            public static readonly string GetParams = "get_params";
            /// <summary>
            /// The query string
            /// </summary>
            public static readonly string QueryString = "query_string";
            /// <summary>
            /// The post-parameters
            /// </summary>
            public static readonly string PostParams = "post_params";
            /// <summary>
            /// The post body
            /// </summary>
            public static readonly string PostBody = "post_body";
            /// <summary>
            /// The user IP
            /// </summary>
            public static readonly string UserIp = "user_ip";
        }

        /// <summary>
        /// Gets or sets the URL.
        /// </summary>
        /// <value>
        /// The URL.
        /// </value>
        public string Url
        {
            get { return this._keyedValues[ReservedProperties.Url] as string; }
            set { this._keyedValues[ReservedProperties.Url] = value; }
        }

        /// <summary>
        /// Gets or sets the method.
        /// </summary>
        /// <value>
        /// The method.
        /// </value>
        public string Method
        {
            get { return this._keyedValues[ReservedProperties.Method] as string; }
            set { this._keyedValues[ReservedProperties.Method] = value; }
        }

        /// <summary>
        /// Gets or sets the headers.
        /// </summary>
        /// <value>
        /// The headers.
        /// </value>
        public IDictionary<string, string> Headers
        {
            get { return this._keyedValues[ReservedProperties.Headers] as IDictionary<string, string>; }
            set { this._keyedValues[ReservedProperties.Headers] = value; }
        }

        /// <summary>
        /// Gets or sets the parameters.
        /// </summary>
        /// <value>
        /// The parameters.
        /// </value>
        public IDictionary<string, object> Params
        {
            get { return this._keyedValues[ReservedProperties.Params] as IDictionary<string, object>; }
            set { this._keyedValues[ReservedProperties.Params] = value; }
        }

        /// <summary>
        /// Gets or sets the get-parameters.
        /// </summary>
        /// <value>
        /// The get-parameters.
        /// </value>
        public IDictionary<string, object> GetParams
        {
            get { return this._keyedValues[ReservedProperties.GetParams] as IDictionary<string, object>; }
            set { this._keyedValues[ReservedProperties.GetParams] = value; }
        }

        /// <summary>
        /// Gets or sets the query string.
        /// </summary>
        /// <value>
        /// The query string.
        /// </value>
        public string QueryString
        {
            get { return this._keyedValues[ReservedProperties.QueryString] as string; }
            set { this._keyedValues[ReservedProperties.QueryString] = value; }
        }

        /// <summary>
        /// Gets or sets the post-parameters.
        /// </summary>
        /// <value>
        /// The post-parameters.
        /// </value>
        public IDictionary<string, object> PostParams
        {
            get { return this._keyedValues[ReservedProperties.PostParams] as IDictionary<string, object>; }
            set { this._keyedValues[ReservedProperties.PostParams] = value; }
        }

        /// <summary>
        /// Gets or sets the post-body.
        /// </summary>
        /// <value>
        /// The post-body.
        /// </value>
        public string PostBody
        {
            get { return this._keyedValues[ReservedProperties.PostBody] as string; }
            set { this._keyedValues[ReservedProperties.PostBody] = value; }
        }

        /// <summary>
        /// Gets or sets the user IP.
        /// </summary>
        /// <value>
        /// The user IP.
        /// </value>
        public string UserIp
        {
            get { return this._keyedValues[ReservedProperties.UserIp] as string; }
            set { this._keyedValues[ReservedProperties.UserIp] = value; }
        }

    }
}
