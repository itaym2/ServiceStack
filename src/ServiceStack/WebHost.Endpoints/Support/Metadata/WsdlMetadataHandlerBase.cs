using System;
using System.Web;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.Logging;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.WebHost.Endpoints.Metadata;
using ServiceStack.WebHost.Endpoints.Support.Templates;

namespace ServiceStack.WebHost.Endpoints.Support.Metadata
{
	public abstract class WsdlMetadataHandlerBase : HttpHandlerBase
	{
		private readonly ILog log = LogManager.GetLogger(typeof(WsdlMetadataHandlerBase));

		protected abstract WsdlTemplateBase GetWsdlTemplate();

		public override void Execute(HttpContext context)
		{
			EndpointHost.Config.AssertFeatures(Feature.Metadata);

			context.Response.ContentType = "text/xml";

			var baseUri = context.Request.GetParentBaseUrl();
			var optimizeForFlash = context.Request.QueryString["flash"] != null;
			var includeAllTypesInAssembly = context.Request.QueryString["includeAllTypes"] != null;
            var operations = new XsdMetadata(
                EndpointHost.Metadata, flash: optimizeForFlash, includeAllTypes: includeAllTypesInAssembly);

			try
			{
				var wsdlTemplate = GetWsdlTemplate(operations, baseUri, optimizeForFlash, includeAllTypesInAssembly, context.Request.GetBaseUrl());
				context.Response.Write(wsdlTemplate.ToString());
			}
			catch (Exception ex)
			{
				log.Error("Autogeneration of WSDL failed.", ex);

				context.Response.Write("Autogenerated WSDLs are not supported "
					+ (Env.IsMono ? "on Mono" : "with this configuration"));
			}
		}

		public void Execute(IHttpRequest httpReq, IHttpResponse httpRes)
		{
			EndpointHost.Config.AssertFeatures(Feature.Metadata);

			httpRes.ContentType = "text/xml";

			var baseUri = httpReq.GetParentBaseUrl();
			var optimizeForFlash = httpReq.QueryString["flash"] != null;
			var includeAllTypesInAssembly = httpReq.QueryString["includeAllTypes"] != null;
		    var operations = new XsdMetadata(
                EndpointHost.Metadata, flash: optimizeForFlash, includeAllTypes: includeAllTypesInAssembly);

			try
			{
				var wsdlTemplate = GetWsdlTemplate(operations, baseUri, optimizeForFlash, includeAllTypesInAssembly, httpReq.GetBaseUrl());
				httpRes.Write(wsdlTemplate.ToString());
			}
			catch (Exception ex)
			{
				log.Error("Autogeneration of WSDL failed.", ex);

				httpRes.Write("Autogenerated WSDLs are not supported "
					+ (Env.IsMono ? "on Mono" : "with this configuration"));
			}
		}

        public WsdlTemplateBase GetWsdlTemplate(XsdMetadata operations, string baseUri, bool optimizeForFlash, bool includeAllTypesInAssembly, string rawUrl)
		{
			var xsd = new XsdGenerator {
                OperationTypes = operations.GetAllTypes(),
				OptimizeForFlash = optimizeForFlash,
				IncludeAllTypesInAssembly = includeAllTypesInAssembly,
			}.ToString();

			var wsdlTemplate = GetWsdlTemplate();
			wsdlTemplate.Xsd = xsd;
            wsdlTemplate.ReplyOperationNames = operations.GetReplyOperationNames();
            wsdlTemplate.OneWayOperationNames = operations.GetOneWayOperationNames();

			if (rawUrl.ToLower().StartsWith(baseUri))
			{
				wsdlTemplate.ReplyEndpointUri = rawUrl;
				wsdlTemplate.OneWayEndpointUri = rawUrl;
			}
			else
			{
				var suffix = GetType().Name.StartsWith("Soap11") ? "soap11" : "soap12";
				wsdlTemplate.ReplyEndpointUri = baseUri + suffix;
				wsdlTemplate.OneWayEndpointUri = baseUri + suffix;
			}

			return wsdlTemplate;
		}
	}
}