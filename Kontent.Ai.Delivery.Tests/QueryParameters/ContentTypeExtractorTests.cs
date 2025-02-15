﻿using System.Collections.Generic;
using AngleSharp.Html.Parser;
using FakeItEasy;
using Kontent.Ai.Delivery.Abstractions;
using Kontent.Ai.Delivery.ContentItems;
using Kontent.Ai.Delivery.Tests.Factories;
using Kontent.Ai.Urls.Delivery.QueryParameters;
using Kontent.Ai.Urls.Delivery.QueryParameters.Filters;
using Xunit;

namespace Kontent.Ai.Delivery.Tests.QueryParameters
{
    public class ContentTypeExtractorTests
    {
        private const string CONTENT_TYPE_CODENAME = "SomeContentType";
        private const string FAKE_PROJECT_ID = "00000000-0000-0000-0000-000000000000";

        private readonly DeliveryClient _client;

        public ContentTypeExtractorTests()
        {
            var contentTypeProvider = A.Fake<ITypeProvider>();

            A.CallTo(() => contentTypeProvider.GetCodename(typeof(TypeWithContentTypeCodename))).Returns(TypeWithContentTypeCodename.Codename);
            A.CallTo(() => contentTypeProvider.GetCodename(typeof(TypeWithoutContentTypeCodename))).Returns(null);

            var deliveryOptions = DeliveryOptionsFactory.CreateMonitor(new DeliveryOptions { ProjectId = FAKE_PROJECT_ID });
            var modelProvider = new ModelProvider(null, null, contentTypeProvider, null, new DeliveryJsonSerializer(), new HtmlParser(), deliveryOptions);
            _client = new DeliveryClient(
                deliveryOptions,
                modelProvider,
                null,
                contentTypeProvider
            );
        }

        private class TypeWithContentTypeCodename
        {
            public const string Codename = CONTENT_TYPE_CODENAME;
        }

        private class TypeWithoutContentTypeCodename
        {
            public const int Answer = 42;
        }

        [Fact]
        public void ExtractParameters_WhenGivenTypeWithCodenameAndExistingParams_AddsCodenameToParams()
        {
            var existingParams = new List<IQueryParameter> { new SkipParameter(15) };

            var enhancedParams = new List<IQueryParameter>(_client.EnsureContentTypeFilter<TypeWithContentTypeCodename>(existingParams));

            Assert.Equal(2, enhancedParams.Count);
            Assert.True(enhancedParams.Find(x => x.GetQueryStringParameter() == $"system.type={CONTENT_TYPE_CODENAME}") != null);
        }

        [Fact]
        public void ExtractParameters_WhenGivenTypeWithCodename_CreatesNewParams()
        {
            var enhancedParams = new List<IQueryParameter>(_client.EnsureContentTypeFilter<TypeWithContentTypeCodename>());

            Assert.Single(enhancedParams);
            Assert.True(enhancedParams.Find(x => x.GetQueryStringParameter() == $"system.type={CONTENT_TYPE_CODENAME}") != null);
        }

        [Fact]
        public void ExtractParameters_WhenGivenTypeWithoutCodenameNoParams_CreatesEmptyParams()
        {
            var enhancedParams = new List<IQueryParameter>(_client.EnsureContentTypeFilter<TypeWithoutContentTypeCodename>());

            Assert.Empty(enhancedParams);
        }

        [Fact]
        public void ExtractParameters_WhenGivenTypeWithoutCodenameAndParams_ReturnsParams()
        {
            var existingParams = new List<IQueryParameter> { new SkipParameter(15) };

            var enhancedParams = new List<IQueryParameter>(_client.EnsureContentTypeFilter<TypeWithoutContentTypeCodename>(existingParams));

            Assert.Single(enhancedParams);
            Assert.True(enhancedParams.Find(x => x.GetQueryStringParameter() == "system.type=TypeWithoutContentTypeCodename") == null);
        }

        [Fact]
        public void ExtractParameters_WhenGivenTypeWithCodenameAndExistingTypeParameter_DoesNotAddCodenameToParams()
        {
            var existingParams = new List<IQueryParameter> { new EqualsFilter("system.type", CONTENT_TYPE_CODENAME) };

            var enhancedParams = new List<IQueryParameter>(_client.EnsureContentTypeFilter<TypeWithContentTypeCodename>(existingParams));

            Assert.Single(enhancedParams);
            Assert.True(enhancedParams.Find(x => x.GetQueryStringParameter() == $"system.type%5Beq%5D={CONTENT_TYPE_CODENAME}") != null);
        }
    }
}
