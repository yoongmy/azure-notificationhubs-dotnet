//------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved. 
// Licensed under the MIT License. See License.txt in the project root for 
// license information.
//------------------------------------------------------------

using RichardSzalay.MockHttp;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace Microsoft.Azure.NotificationHubs.Tests
{
    using Extensions.Configuration;
    using Messaging;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit;

    public class NotificationHubClientUnitTest
    {
        private const string HubConnectionString = "Endpoint=sb://sdk-unit-tests-namespace.servicebus.windows.net/;SharedAccessKeyName=DefaultFullSharedAccessSignature;SharedAccessKey=1234a=";
        private const string HubName = "sdk-unit-tests-hub";
        private const string BaseUri = "https://sdk-unit-tests-namespace.servicebus.windows.net/sdk-unit-tests-hub";

        private const string ApiVersion = "2017-04";

        private const string AuthorizationScheme = "SharedAccessSignature";
        private const string AuthorizationParameterRegex =
            @"sr=http%3a%2f%2fsdk-unit-tests-namespace\.servicebus\.windows\.net%2fsdk-unit-tests-hub%2fregistrations%2f&sig=[^=]+=\d+&skn=DefaultFullSharedAccessSignature";

        private const string AdmDeviceToken = "amzn1.adm-registration.v2.123";
        private const string AppleDeviceToken = "1111111111111111111111111111111111111111111111111111111111111111";
        private const string BaiduUserId = "userId";
        private const string BaiduChannelId = "channelId";
        private const string GcmDeviceToken = "gcm.registration.v2.123";
        private const string MpnsDeviceToken = "https://some.url";

        private readonly IConfigurationRoot _configuration;
        private readonly NotificationHubClient _hubClient;
        private readonly MockHttpMessageHandler _mockHandler;

        public NotificationHubClientUnitTest()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            _configuration = builder.Build();

            _mockHandler = new MockHttpMessageHandler();
            _mockHandler.Fallback.Respond(request => throw new InvalidOperationException("No matching mock handler"));
            var httpClient = new HttpClient(_mockHandler);
            _hubClient = new NotificationHubClient(HubConnectionString, HubName, httpClient);
        }

        private MockedRequest WhenRequested(HttpMethod httpMethod, string uri)
        {
            return _mockHandler
                .When(httpMethod, uri)
                .WithQueryString("api-version", ApiVersion)
                .With(request => request.Headers.Authorization.Scheme == AuthorizationScheme &&
                                 Regex.IsMatch(request.Headers.Authorization.Parameter, AuthorizationParameterRegex));
        }

        [Fact]
        public async Task CreateRegistrationAsync_PassValidAdmNativeRegistration_GetCreatedRegistrationBack()
        {
            WhenRequested(HttpMethod.Post, $"{BaseUri}/registrations")
                .WithContent(@"<entry xmlns=""http://www.w3.org/2005/Atom""><content type=""application/xml""><AdmRegistrationDescription xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.microsoft.com/netservices/2010/10/servicebus/connect""><RegistrationId i:nil=""true"" /><Tags>tag1</Tags><PushVariables>{""var1"":""value1""}</PushVariables><AdmRegistrationId>amzn1.adm-registration.v2.123</AdmRegistrationId></AdmRegistrationDescription></content></entry>")
                .Respond(_ => 
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            @"<entry a:etag=""W/&quot;1&quot;"" xmlns=""http://www.w3.org/2005/Atom"" xmlns:a=""http://schemas.microsoft.com/ado/2007/08/dataservices/metadata""><id>https://sdk-sample-namespace.servicebus.windows.net/sdk-sample-nh/registrations/3648713736635612734-1576008139854428372-1?api-version=2017-04</id><title type=""text"">3648713736635612734-1576008139854428372-1</title><published>2018-11-06T14:09:58Z</published><updated>2018-11-06T14:09:58Z</updated><link rel=""self"" href=""https://sdk-sample-namespace.servicebus.windows.net/sdk-sample-nh/registrations/3648713736635612734-1576008139854428372-1?api-version=2017-04""/><content type=""application/xml""><AdmRegistrationDescription xmlns=""http://schemas.microsoft.com/netservices/2010/10/servicebus/connect"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><ETag>1</ETag><ExpirationTime>9999-12-31T23:59:59.999</ExpirationTime><RegistrationId>3648713736635612734-1576008139854428372-1</RegistrationId><Tags>tag1</Tags><PushVariables>{""var1"":""value1""}</PushVariables><AdmRegistrationId>amzn1.adm-registration.v2.123</AdmRegistrationId></AdmRegistrationDescription></content></entry>")
                    });

            var registration = new AdmRegistrationDescription(AdmDeviceToken)
            {
                PushVariables = new Dictionary<string, string> {{"var1", "value1"}},
                Tags = new HashSet<string>() {"tag1"}
            };

            var createdRegistration = await _hubClient.CreateRegistrationAsync(registration);

            Assert.NotNull(createdRegistration.RegistrationId);
            Assert.NotNull(createdRegistration.ETag);
            Assert.NotNull(createdRegistration.ExpirationTime);
            Assert.Contains(new KeyValuePair<string, string>("var1", "value1"), createdRegistration.PushVariables);
            Assert.Contains("tag1", createdRegistration.Tags);
            Assert.Equal(registration.AdmRegistrationId, createdRegistration.AdmRegistrationId);
        }

        [Fact]
        public async Task CreateRegistrationAsync_PassValidAdmTemplateRegistration_GetCreatedRegistrationBack()
        {
            WhenRequested(HttpMethod.Post, $"{BaseUri}/registrations")
                .WithContent(
                    @"<entry xmlns=""http://www.w3.org/2005/Atom""><content type=""application/xml""><AdmTemplateRegistrationDescription xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.microsoft.com/netservices/2010/10/servicebus/connect""><RegistrationId i:nil=""true"" /><Tags>tag1</Tags><PushVariables>{""var1"":""value1""}</PushVariables><AdmRegistrationId>amzn1.adm-registration.v2.123</AdmRegistrationId><BodyTemplate><![CDATA[{""data"":{""key1"":""value1""}}]]></BodyTemplate><TemplateName>Template Name</TemplateName></AdmTemplateRegistrationDescription></content></entry>")
                .Respond(_ =>
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            @"<entry a:etag=""W/&quot;1&quot;"" xmlns=""http://www.w3.org/2005/Atom"" xmlns:a=""http://schemas.microsoft.com/ado/2007/08/dataservices/metadata""><id>https://sdk-sample-namespace.servicebus.windows.net/sdk-sample-nh/registrations/8536489232329389202-7020937479475399572-1?api-version=2017-04</id><title type=""text"">8536489232329389202-7020937479475399572-1</title><published>2018-11-06T16:34:10Z</published><updated>2018-11-06T16:34:10Z</updated><link rel=""self"" href=""https://sdk-sample-namespace.servicebus.windows.net/sdk-sample-nh/registrations/8536489232329389202-7020937479475399572-1?api-version=2017-04""/><content type=""application/xml""><AdmTemplateRegistrationDescription xmlns=""http://schemas.microsoft.com/netservices/2010/10/servicebus/connect"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><ETag>1</ETag><ExpirationTime>9999-12-31T23:59:59.999</ExpirationTime><RegistrationId>8536489232329389202-7020937479475399572-1</RegistrationId><Tags>tag1</Tags><PushVariables>{""var1"":""value1""}</PushVariables><AdmRegistrationId>amzn1.adm-registration.v2.123</AdmRegistrationId><BodyTemplate><![CDATA[{""data"":{""key1"":""value1""}}]]></BodyTemplate><TemplateName>Template Name</TemplateName></AdmTemplateRegistrationDescription></content></entry>")
                    });

            var registration = new AdmTemplateRegistrationDescription(AdmDeviceToken, "{\"data\":{\"key1\":\"value1\"}}")
            {
                PushVariables = new Dictionary<string, string>() {{"var1", "value1"}},
                Tags = new HashSet<string> {"tag1"},
                TemplateName = "Template Name"
            };

            var createdRegistration = await _hubClient.CreateRegistrationAsync(registration);

            Assert.NotNull(createdRegistration.RegistrationId);
            Assert.NotNull(createdRegistration.ETag);
            Assert.NotNull(createdRegistration.ExpirationTime);
            Assert.Contains(new KeyValuePair<string, string>("var1", "value1"), createdRegistration.PushVariables);
            Assert.Contains("tag1", createdRegistration.Tags);
            Assert.Equal(registration.AdmRegistrationId, createdRegistration.AdmRegistrationId);
            Assert.Equal(registration.BodyTemplate.Value, createdRegistration.BodyTemplate.Value);
            Assert.Equal(registration.TemplateName, createdRegistration.TemplateName);
        }

        [Fact]
        public async Task CreateRegistrationAsync_PassValidAppleNativeRegistration_GetCreatedRegistrationBack()
        {
            WhenRequested(HttpMethod.Post, $"{BaseUri}/registrations")
                .WithContent(
                    @"<entry xmlns=""http://www.w3.org/2005/Atom""><content type=""application/xml""><AppleRegistrationDescription xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.microsoft.com/netservices/2010/10/servicebus/connect""><RegistrationId i:nil=""true"" /><Tags>tag1</Tags><PushVariables>{""var1"":""value1""}</PushVariables><DeviceToken>1111111111111111111111111111111111111111111111111111111111111111</DeviceToken></AppleRegistrationDescription></content></entry>")
                .Respond(_ => 
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            @"<entry a:etag=""W/&quot;1&quot;"" xmlns=""http://www.w3.org/2005/Atom"" xmlns:a=""http://schemas.microsoft.com/ado/2007/08/dataservices/metadata""><id>https://sdk-sample-namespace.servicebus.windows.net/sdk-sample-nh/registrations/2772199343190529178-5408359458679310168-2?api-version=2017-04</id><title type=""text"">2772199343190529178-5408359458679310168-2</title><published>2018-11-06T16:44:09Z</published><updated>2018-11-06T16:44:09Z</updated><link rel=""self"" href=""https://sdk-sample-namespace.servicebus.windows.net/sdk-sample-nh/registrations/2772199343190529178-5408359458679310168-2?api-version=2017-04""/><content type=""application/xml""><AppleRegistrationDescription xmlns=""http://schemas.microsoft.com/netservices/2010/10/servicebus/connect"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><ETag>1</ETag><ExpirationTime>9999-12-31T23:59:59.999</ExpirationTime><RegistrationId>2772199343190529178-5408359458679310168-2</RegistrationId><Tags>tag1</Tags><PushVariables>{""var1"":""value1""}</PushVariables><DeviceToken>1111111111111111111111111111111111111111111111111111111111111111</DeviceToken></AppleRegistrationDescription></content></entry>")
                    });

            var registration = new AppleRegistrationDescription(AppleDeviceToken)
            {
                PushVariables = new Dictionary<string, string> {{"var1", "value1"}},
                Tags = new HashSet<string> {"tag1"}
            };

            var createdRegistration = await _hubClient.CreateRegistrationAsync(registration);

            Assert.NotNull(createdRegistration.RegistrationId);
            Assert.NotNull(createdRegistration.ETag);
            Assert.NotNull(createdRegistration.ExpirationTime);
            Assert.Contains(new KeyValuePair<string, string>("var1", "value1"), createdRegistration.PushVariables);
            Assert.Contains("tag1", createdRegistration.Tags);
            Assert.Equal(registration.DeviceToken, createdRegistration.DeviceToken);
        }

        [Fact]
        public async Task CreateRegistrationAsync_PassValidAppleTemplateRegistration_GetCreatedRegistrationBack()
        {
            WhenRequested(HttpMethod.Post, $"{BaseUri}/registrations")
                .WithContent(
                    @"<entry xmlns=""http://www.w3.org/2005/Atom""><content type=""application/xml""><AppleTemplateRegistrationDescription xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.microsoft.com/netservices/2010/10/servicebus/connect""><RegistrationId i:nil=""true"" /><Tags>tag1</Tags><PushVariables>{""var1"":""value1""}</PushVariables><DeviceToken>1111111111111111111111111111111111111111111111111111111111111111</DeviceToken><BodyTemplate><![CDATA[{""aps"":{""alert"":""alert!""}}]]></BodyTemplate><Expiry i:nil=""true"" /><TemplateName>Template Name</TemplateName><ApnsHeaders /></AppleTemplateRegistrationDescription></content></entry>")
                .Respond(_ => 
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            @"<entry a:etag=""W/&quot;1&quot;"" xmlns=""http://www.w3.org/2005/Atom"" xmlns:a=""http://schemas.microsoft.com/ado/2007/08/dataservices/metadata""><id>https://sdk-sample-namespace.servicebus.windows.net/sdk-sample-nh/registrations/170448309501312030-7068010450960759587-1?api-version=2017-04</id><title type=""text"">170448309501312030-7068010450960759587-1</title><published>2018-11-06T16:56:12Z</published><updated>2018-11-06T16:56:12Z</updated><link rel=""self"" href=""https://sdk-sample-namespace.servicebus.windows.net/sdk-sample-nh/registrations/170448309501312030-7068010450960759587-1?api-version=2017-04""/><content type=""application/xml""><AppleTemplateRegistrationDescription xmlns=""http://schemas.microsoft.com/netservices/2010/10/servicebus/connect"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><ETag>1</ETag><ExpirationTime>9999-12-31T23:59:59.999</ExpirationTime><RegistrationId>170448309501312030-7068010450960759587-1</RegistrationId><Tags>tag1</Tags><PushVariables>{""var1"":""value1""}</PushVariables><DeviceToken>1111111111111111111111111111111111111111111111111111111111111111</DeviceToken><BodyTemplate><![CDATA[{""aps"":{""alert"":""alert!""}}]]></BodyTemplate><Expiry i:nil=""true""/><TemplateName>Template Name</TemplateName><ApnsHeaders/></AppleTemplateRegistrationDescription></content></entry>")
                    });

            var registration =
                new AppleTemplateRegistrationDescription(AppleDeviceToken, "{\"aps\":{\"alert\":\"alert!\"}}")
                {
                    PushVariables = new Dictionary<string, string> {{"var1", "value1"}},
                    Tags = new HashSet<string> {"tag1"},
                    TemplateName = "Template Name"
                };

            var createdRegistration = await _hubClient.CreateRegistrationAsync(registration);

            Assert.NotNull(createdRegistration.RegistrationId);
            Assert.NotNull(createdRegistration.ETag);
            Assert.NotNull(createdRegistration.ExpirationTime);
            Assert.Contains(new KeyValuePair<string, string>("var1", "value1"), createdRegistration.PushVariables);
            Assert.Contains("tag1", createdRegistration.Tags);
            Assert.Equal(registration.DeviceToken, createdRegistration.DeviceToken);
            Assert.Equal(registration.BodyTemplate.Value, createdRegistration.BodyTemplate.Value);
            Assert.Equal(registration.TemplateName, createdRegistration.TemplateName);
        }

        [Fact]
        public async Task CreateRegistrationAsync_PassValidBaiduNativeRegistration_GetCreatedRegistrationBack()
        {
            WhenRequested(HttpMethod.Post, $"{BaseUri}/registrations")
                .WithContent(
                    @"")
                .Respond(_ =>
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            @"")
                    });

            var registration = new BaiduRegistrationDescription(BaiduUserId, BaiduChannelId, new[] {"tag1"})
            {
                PushVariables = new Dictionary<string, string> {{"var1", "value1"}}
            };

            var createdRegistration = await _hubClient.CreateRegistrationAsync(registration);

            Assert.NotNull(createdRegistration.RegistrationId);
            Assert.NotNull(createdRegistration.ETag);
            Assert.NotNull(createdRegistration.ExpirationTime);
            Assert.Contains(new KeyValuePair<string, string>("var1", "value1"), createdRegistration.PushVariables);
            Assert.Contains("tag1", createdRegistration.Tags);
            Assert.Equal(registration.BaiduUserId, createdRegistration.BaiduUserId);
            Assert.Equal(registration.BaiduChannelId, createdRegistration.BaiduChannelId);
        }

        [Fact]
        public async Task CreateRegistrationAsync_PassValidBaiduTemplateRegistration_GetCreatedRegistrationBack()
        {
            WhenRequested(HttpMethod.Post, $"{BaseUri}/registrations")
                .WithContent(
                    @"")
                .Respond(_ => 
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            @"")
                    });

            var registration = new BaiduTemplateRegistrationDescription(
                BaiduUserId, 
                BaiduChannelId, 
                "{\"title\":\"Title\",\"description\":\"Description\"}",
                new[] {"tag1"})
            {
                PushVariables = new Dictionary<string, string>() {{"var1", "value1"}}, TemplateName = "Template Name"
            };

            var createdRegistration = await _hubClient.CreateRegistrationAsync(registration);

            Assert.NotNull(createdRegistration.RegistrationId);
            Assert.NotNull(createdRegistration.ETag);
            Assert.NotNull(createdRegistration.ExpirationTime);
            Assert.Contains(new KeyValuePair<string, string>("var1", "value1"), createdRegistration.PushVariables);
            Assert.Contains("tag1", createdRegistration.Tags);
            Assert.Equal(registration.BaiduUserId, createdRegistration.BaiduUserId);
            Assert.Equal(registration.BaiduChannelId, createdRegistration.BaiduChannelId);
            Assert.Equal(registration.BodyTemplate.Value, createdRegistration.BodyTemplate.Value);
            Assert.Equal(registration.TemplateName, createdRegistration.TemplateName);
        }

        [Fact]
        public async Task CreateRegistrationAsync_PassValidGcmNativeRegistration_GetCreatedRegistrationBack()
        {
            WhenRequested(HttpMethod.Post, $"{BaseUri}/registrations")
                .WithContent(
                    @"<entry xmlns=""http://www.w3.org/2005/Atom""><content type=""application/xml""><GcmRegistrationDescription xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.microsoft.com/netservices/2010/10/servicebus/connect""><RegistrationId i:nil=""true"" /><Tags>tag1</Tags><PushVariables>{""var1"":""value1""}</PushVariables><GcmRegistrationId>gcm.registration.v2.123</GcmRegistrationId></GcmRegistrationDescription></content></entry>")
                .Respond(_ => 
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            @"<entry a:etag=""W/&quot;1&quot;"" xmlns=""http://www.w3.org/2005/Atom"" xmlns:a=""http://schemas.microsoft.com/ado/2007/08/dataservices/metadata""><id>https://sdk-sample-namespace.servicebus.windows.net/sdk-sample-nh/registrations/8504578834943685912-5323981290966071257-3?api-version=2017-04</id><title type=""text"">8504578834943685912-5323981290966071257-3</title><published>2018-11-07T12:44:57Z</published><updated>2018-11-07T12:44:57Z</updated><link rel=""self"" href=""https://sdk-sample-namespace.servicebus.windows.net/sdk-sample-nh/registrations/8504578834943685912-5323981290966071257-3?api-version=2017-04""/><content type=""application/xml""><GcmRegistrationDescription xmlns=""http://schemas.microsoft.com/netservices/2010/10/servicebus/connect"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><ETag>1</ETag><ExpirationTime>9999-12-31T23:59:59.999</ExpirationTime><RegistrationId>8504578834943685912-5323981290966071257-3</RegistrationId><Tags>tag1</Tags><PushVariables>{""var1"":""value1""}</PushVariables><GcmRegistrationId>gcm.registration.v2.123</GcmRegistrationId></GcmRegistrationDescription></content></entry>")
                    });

            var registration = new GcmRegistrationDescription(GcmDeviceToken)
            {
                PushVariables = new Dictionary<string, string> {{"var1", "value1"}},
                Tags = new HashSet<string> {"tag1"}
            };

            var createdRegistration = await _hubClient.CreateRegistrationAsync(registration);

            Assert.NotNull(createdRegistration.RegistrationId);
            Assert.NotNull(createdRegistration.ETag);
            Assert.NotNull(createdRegistration.ExpirationTime);
            Assert.Contains(new KeyValuePair<string, string>("var1", "value1"), createdRegistration.PushVariables);
            Assert.Contains("tag1", createdRegistration.Tags);
            Assert.Equal(registration.GcmRegistrationId, createdRegistration.GcmRegistrationId);
        }

        [Fact]
        public async Task CreateRegistrationAsync_PassValidGcmTemplateRegistration_GetCreatedRegistrationBack()
        {
            WhenRequested(HttpMethod.Post, $"{BaseUri}/registrations")
                .WithContent(
                    @"<entry xmlns=""http://www.w3.org/2005/Atom""><content type=""application/xml""><GcmTemplateRegistrationDescription xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" xmlns=""http://schemas.microsoft.com/netservices/2010/10/servicebus/connect""><RegistrationId i:nil=""true"" /><Tags>tag1</Tags><PushVariables>{""var1"":""value1""}</PushVariables><GcmRegistrationId>gcm.registration.v2.123</GcmRegistrationId><BodyTemplate><![CDATA[{""data"":{""message"":""Message""}}]]></BodyTemplate><TemplateName>Template Name</TemplateName></GcmTemplateRegistrationDescription></content></entry>")
                .Respond(_ =>
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            @"<entry a:etag=""W/&quot;1&quot;"" xmlns=""http://www.w3.org/2005/Atom"" xmlns:a=""http://schemas.microsoft.com/ado/2007/08/dataservices/metadata""><id>https://sdk-sample-namespace.servicebus.windows.net/sdk-sample-nh/registrations/7575034470467616854-5119800720011323415-1?api-version=2017-04</id><title type=""text"">7575034470467616854-5119800720011323415-1</title><published>2018-11-07T13:02:13Z</published><updated>2018-11-07T13:02:13Z</updated><link rel=""self"" href=""https://sdk-sample-namespace.servicebus.windows.net/sdk-sample-nh/registrations/7575034470467616854-5119800720011323415-1?api-version=2017-04""/><content type=""application/xml""><GcmTemplateRegistrationDescription xmlns=""http://schemas.microsoft.com/netservices/2010/10/servicebus/connect"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance""><ETag>1</ETag><ExpirationTime>9999-12-31T23:59:59.999</ExpirationTime><RegistrationId>7575034470467616854-5119800720011323415-1</RegistrationId><Tags>tag1</Tags><PushVariables>{""var1"":""value1""}</PushVariables><GcmRegistrationId>gcm.registration.v2.123</GcmRegistrationId><BodyTemplate><![CDATA[{""data"":{""message"":""Message""}}]]></BodyTemplate><TemplateName>Template Name</TemplateName></GcmTemplateRegistrationDescription></content></entry>")
                    });

            var registration =
                new GcmTemplateRegistrationDescription(GcmDeviceToken, "{\"data\":{\"message\":\"Message\"}}")
                {
                    PushVariables = new Dictionary<string, string> {{"var1", "value1"}},
                    Tags = new HashSet<string> {"tag1"},
                    TemplateName = "Template Name"
                };

            var createdRegistration = await _hubClient.CreateRegistrationAsync(registration);

            Assert.NotNull(createdRegistration.RegistrationId);
            Assert.NotNull(createdRegistration.ETag);
            Assert.NotNull(createdRegistration.ExpirationTime);
            Assert.Contains(new KeyValuePair<string, string>("var1", "value1"), createdRegistration.PushVariables);
            Assert.Contains("tag1", createdRegistration.Tags);
            Assert.Equal(registration.GcmRegistrationId, createdRegistration.GcmRegistrationId);
            Assert.Equal(registration.BodyTemplate.Value, createdRegistration.BodyTemplate.Value);
            Assert.Equal(registration.TemplateName, createdRegistration.TemplateName);
        }

        [Fact]
        public async Task CreateRegistrationAsync_PassValidMpnsNativeRegistration_GetCreatedRegistrationBack()
        {
            WhenRequested(HttpMethod.Post, $"{BaseUri}/registrations")
                .WithContent(
                    @"")
                .Respond(_ =>
                    new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(
                            @"")
                    });

            var registration = new MpnsRegistrationDescription(MpnsDeviceToken)
            {
                PushVariables = new Dictionary<string, string> {{"var1", "value1"}},
                Tags = new HashSet<string> {"tag1"},
                SecondaryTileName = "Tile name"
            };

            var createdRegistration = await _hubClient.CreateRegistrationAsync(registration);

            Assert.NotNull(createdRegistration.RegistrationId);
            Assert.NotNull(createdRegistration.ETag);
            Assert.NotNull(createdRegistration.ExpirationTime);
            Assert.Contains(new KeyValuePair<string, string>("var1", "value1"), createdRegistration.PushVariables);
            Assert.Contains("tag1", createdRegistration.Tags);
            Assert.Equal(registration.ChannelUri, createdRegistration.ChannelUri);
            Assert.Equal(registration.SecondaryTileName, createdRegistration.SecondaryTileName);
        }

        [Fact]
        public async Task CreateRegistrationAsync_PassValidMpnsTemplateRegistration_GetCreatedRegistrationBack()
        {
            var registration = new MpnsTemplateRegistrationDescription(_configuration["MpnsDeviceToken"], "<wp:Notification xmlns:wp=\"WPNotification\" Version=\"2.0\"><wp:Tile Id=\"TileId\" Template=\"IconicTile\"><wp:Title Action=\"Clear\">Title</wp:Title></wp:Tile></wp:Notification>", new[] { "tag1" });
            registration.PushVariables = new Dictionary<string, string>()
            {
                {"var1", "value1"}
            };
            registration.SecondaryTileName = "Tile name";
            registration.TemplateName = "Template Name";

            var createdRegistration = await _hubClient.CreateRegistrationAsync(registration);

            Assert.NotNull(createdRegistration.RegistrationId);
            Assert.NotNull(createdRegistration.ETag);
            Assert.NotNull(createdRegistration.ExpirationTime);
            Assert.Contains(new KeyValuePair<string, string>("var1", "value1"), createdRegistration.PushVariables);
            Assert.Contains("tag1", createdRegistration.Tags);
            Assert.Equal(registration.ChannelUri, createdRegistration.ChannelUri);
            Assert.Equal(registration.SecondaryTileName, createdRegistration.SecondaryTileName);
            Assert.Equal(registration.BodyTemplate.Value, createdRegistration.BodyTemplate.Value);
            Assert.Equal(registration.TemplateName, createdRegistration.TemplateName);
        }

        [Fact]
        public async Task CreateRegistrationAsync_PassValidWindowsNativeRegistration_GetCreatedRegistrationBack()
        {
            var registration = new WindowsRegistrationDescription(_configuration["WindowsDeviceToken"]);
            registration.PushVariables = new Dictionary<string, string>()
            {
                {"var1", "value1"}
            };
            registration.Tags = new HashSet<string>() { "tag1" };
            registration.SecondaryTileName = "Tile name";

            var createdRegistration = await _hubClient.CreateRegistrationAsync(registration);

            Assert.NotNull(createdRegistration.RegistrationId);
            Assert.NotNull(createdRegistration.ETag);
            Assert.NotNull(createdRegistration.ExpirationTime);
            Assert.Contains(new KeyValuePair<string, string>("var1", "value1"), createdRegistration.PushVariables);
            Assert.Contains("tag1", createdRegistration.Tags);
            Assert.Equal(registration.ChannelUri, createdRegistration.ChannelUri);
            Assert.Equal(registration.SecondaryTileName, createdRegistration.SecondaryTileName);
        }

        [Fact]
        public async Task CreateRegistrationAsync_PassValidWindowsTemplateRegistration_GetCreatedRegistrationBack()
        {
            var registration = new WindowsTemplateRegistrationDescription(_configuration["WindowsDeviceToken"], "<toast><visual><binding template=\"ToastText01\"><text id=\"1\">bodyText</text></binding>  </visual></toast>", new[] { "tag1" });
            registration.PushVariables = new Dictionary<string, string>()
            {
                {"var1", "value1"}
            };
            registration.Tags = new HashSet<string>() { "tag1" };
            registration.SecondaryTileName = "Tile name";
            registration.TemplateName = "Template Name";

            var createdRegistration = await _hubClient.CreateRegistrationAsync(registration);

            Assert.NotNull(createdRegistration.RegistrationId);
            Assert.NotNull(createdRegistration.ETag);
            Assert.NotNull(createdRegistration.ExpirationTime);
            Assert.Contains(new KeyValuePair<string, string>("var1", "value1"), createdRegistration.PushVariables);
            Assert.Contains("tag1", createdRegistration.Tags);
            Assert.Equal(registration.ChannelUri, createdRegistration.ChannelUri);
            Assert.Equal(registration.SecondaryTileName, createdRegistration.SecondaryTileName);
            Assert.Equal(registration.BodyTemplate.Value, createdRegistration.BodyTemplate.Value);
            Assert.Equal(registration.TemplateName, createdRegistration.TemplateName);
        }

        [Fact]
        public async Task CreateOrUpdateRegistrationAsync_UpsertAppleNativeRegistration_GetUpsertedRegistrationBack()
        {
            var registration = new AppleRegistrationDescription(_configuration["AppleDeviceToken"]);

            var createdRegistration = await _hubClient.CreateRegistrationAsync(registration);

            createdRegistration.Tags = new HashSet<string>() { "tag1" };

            var updatedRegistration = await _hubClient.CreateOrUpdateRegistrationAsync(createdRegistration);

            Assert.Contains("tag1", updatedRegistration.Tags);
        }

        [Fact]
        public async Task CreateOrUpdateRegistrationAsync_UpsertAppleNativeRegistrationWithCustomId_GetUpsertedRegistrationBack()
        {
            var registration = new AppleRegistrationDescription(_configuration["AppleDeviceToken"]);

            registration.RegistrationId = "123-234-1";

            var createdRegistration = await _hubClient.CreateOrUpdateRegistrationAsync(registration);

            Assert.Equal(registration.RegistrationId, createdRegistration.RegistrationId);
        }

        [Fact]
        public async Task GetAllRegistrationsAsync_CreateTwoRegistrations_GetAllCreatedRegistrations()
        {
            var appleRegistration = new AppleRegistrationDescription(_configuration["AppleDeviceToken"]);
            var gcmRegistration = new GcmRegistrationDescription(_configuration["GcmDeviceToken"]);

            var createdAppleRegistration = await _hubClient.CreateRegistrationAsync(appleRegistration);
            var createdGcmRegistration = await _hubClient.CreateRegistrationAsync(gcmRegistration);

            var allRegistrations = await _hubClient.GetAllRegistrationsAsync(100);
            var allRegistrationIds = allRegistrations.Select(r => r.RegistrationId).ToArray();

            Assert.Equal(2, allRegistrationIds.Count());
            Assert.Contains(createdAppleRegistration.RegistrationId, allRegistrationIds);
            Assert.Contains(createdGcmRegistration.RegistrationId, allRegistrationIds);
        }

        [Fact]
        public async Task GetAllRegistrationsAsync_UsingTopLessThenNumberOfRegistrations_CorrectContinuationTokenIsReturnedThatAllowsToReadAllRegistrations()
        {
            var appleRegistration1 = new AppleRegistrationDescription(_configuration["AppleDeviceToken"]);
            var appleRegistration2 = new AppleRegistrationDescription(_configuration["AppleDeviceToken"]);
            var appleRegistration3 = new AppleRegistrationDescription(_configuration["AppleDeviceToken"]);

            await _hubClient.CreateRegistrationAsync(appleRegistration1);
            await _hubClient.CreateRegistrationAsync(appleRegistration2);
            await _hubClient.CreateRegistrationAsync(appleRegistration3);

            string continuationToken = null;
            var allRegistrationIds = new List<string>();
            var numberOfCalls = 0;

            do
            {
                var registrations = await (continuationToken == null ? _hubClient.GetAllRegistrationsAsync(2) : _hubClient.GetAllRegistrationsAsync(continuationToken, 2));
                continuationToken = registrations.ContinuationToken;
                allRegistrationIds.AddRange(registrations.Select(r => r.RegistrationId));
                numberOfCalls++;
            } while (continuationToken != null);

            Assert.Equal(2, numberOfCalls);
            Assert.Equal(3, allRegistrationIds.Count);
        }

        [Fact]
        public async Task GetRegistrationsByChannelAsync_CreateTwoRegistrationsWithTheSameChannel_GetCreatedRegistrationsWithRequestedChannel()
        {
            var appleRegistration1 = new AppleRegistrationDescription(_configuration["AppleDeviceToken"]);
            var appleRegistration2 = new AppleRegistrationDescription(_configuration["AppleDeviceToken"]);
            var gcmRegistration = new GcmRegistrationDescription(_configuration["GcmDeviceToken"]);

            var createdAppleRegistration1 = await _hubClient.CreateRegistrationAsync(appleRegistration1);
            var createdAppleRegistration2 = await _hubClient.CreateRegistrationAsync(appleRegistration2);
            // Create a registration with another channel to make sure that SDK passes correct channel and two registrations will be returned
            var createdGcmRegistration = await _hubClient.CreateRegistrationAsync(gcmRegistration);

            var allRegistrations = await _hubClient.GetRegistrationsByChannelAsync(_configuration["AppleDeviceToken"], 100);
            var allRegistrationIds = allRegistrations.Select(r => r.RegistrationId).ToArray();

            Assert.Equal(2, allRegistrationIds.Count());
            Assert.Contains(createdAppleRegistration1.RegistrationId, allRegistrationIds);
            Assert.Contains(createdAppleRegistration2.RegistrationId, allRegistrationIds);
        }

        [Fact]
        public async Task GetRegistrationsByTagAsync_CreateTwoRegistrationsWithTheSameTag_GetCreatedRegistrationsWithRequestedTag()
        {
            var appleRegistration1 = new AppleRegistrationDescription(_configuration["AppleDeviceToken"], new[] { "tag1" });
            var appleRegistration2 = new AppleRegistrationDescription(_configuration["AppleDeviceToken"], new[] { "tag1" });
            var gcmRegistration = new GcmRegistrationDescription(_configuration["GcmDeviceToken"]);

            var createdAppleRegistration1 = await _hubClient.CreateRegistrationAsync(appleRegistration1);
            var createdAppleRegistration2 = await _hubClient.CreateRegistrationAsync(appleRegistration2);
            // Create a registration with another channel to make sure that SDK passes correct tag and two registrations will be returned
            var createdGcmRegistration = await _hubClient.CreateRegistrationAsync(gcmRegistration);

            var allRegistrations = await _hubClient.GetRegistrationsByTagAsync("tag1", 100);
            var allRegistrationIds = allRegistrations.Select(r => r.RegistrationId).ToArray();

            Assert.Equal(2, allRegistrationIds.Count());
            Assert.Contains(createdAppleRegistration1.RegistrationId, allRegistrationIds);
            Assert.Contains(createdAppleRegistration2.RegistrationId, allRegistrationIds);
        }

        [Fact]
        public async Task UpdateRegistrationAsync_UpdateAppleNativeRegistration_GetUpdatedRegistrationBack()
        {
            var registration = new AppleRegistrationDescription(_configuration["AppleDeviceToken"]);

            var createdRegistration = await _hubClient.CreateRegistrationAsync(registration);

            createdRegistration.Tags = new HashSet<string>() { "tag1" };

            var updatedRegistration = await _hubClient.UpdateRegistrationAsync(createdRegistration);

            Assert.Contains("tag1", updatedRegistration.Tags);
        }

        [Fact]
        public async Task DeleteRegistrationAsync_DeleteAppleNativeRegistration_RegistrationIsDeleted()
        {
            var registration = new AppleRegistrationDescription(_configuration["AppleDeviceToken"]);

            var createdRegistration = await _hubClient.CreateRegistrationAsync(registration);

            await _hubClient.DeleteRegistrationAsync(createdRegistration);

            await Assert.ThrowsAsync<MessagingEntityNotFoundException>(async () => await _hubClient.GetRegistrationAsync<AppleRegistrationDescription>(createdRegistration.RegistrationId));
        }

        [Fact]
        public async Task DeleteRegistrationAsync_DeleteAppleNativeRegistrationByRegistrationId_RegistrationIsDeleted()
        {
            var registration = new AppleRegistrationDescription(_configuration["AppleDeviceToken"]);

            var createdRegistration = await _hubClient.CreateRegistrationAsync(registration);

            await _hubClient.DeleteRegistrationAsync(createdRegistration.RegistrationId);

            await Assert.ThrowsAsync<MessagingEntityNotFoundException>(async () => await _hubClient.GetRegistrationAsync<AppleRegistrationDescription>(createdRegistration.RegistrationId));
        }

        [Fact]
        public async Task DeleteRegistrationsByChannelAsync_DeleteAppleNativeRegistrationByChannel_RegistrationIsDeleted()
        {
            var registration = new AppleRegistrationDescription(_configuration["AppleDeviceToken"]);

            var createdRegistration = await _hubClient.CreateRegistrationAsync(registration);

            await _hubClient.DeleteRegistrationsByChannelAsync(_configuration["AppleDeviceToken"]);

            await Assert.ThrowsAsync<MessagingEntityNotFoundException>(async () => await _hubClient.GetRegistrationAsync<AppleRegistrationDescription>(createdRegistration.RegistrationId));
        }

        [Fact]
        private async Task CreateRegistrationIdAsync_CallMethod_GetRegistrationId()
        {
            var registrationId = await _hubClient.CreateRegistrationIdAsync();

            Assert.NotNull(registrationId);
        }

        [Fact]
        private async Task CreateOrUpdateInstallationAsync_CreateInstallationWithExpiryInTemplate_GetCreatedInstallationWithExpiryInTemplateBack()
        {
            var installationId = Guid.NewGuid().ToString();

            var installation = new Installation
            {
                InstallationId = installationId,
                Platform = NotificationPlatform.Apns,
                PushChannel = _configuration["AppleDeviceToken"],
                PushVariables = new Dictionary<string, string> { { "var1", "value1" } },
                Tags = new List<string> { "tag1" },
                Templates = new Dictionary<string, InstallationTemplate>
                {
                    {
                        "Template Name", new InstallationTemplate
                        {
                            Body = "{\"aps\":{\"alert\":\"alert!\"}}",
                            Expiry = DateTime.Now.AddDays(1).ToString("o")
                        }
                    }
                }
            };

            await _hubClient.CreateOrUpdateInstallationAsync(installation);

            await Task.Delay(TimeSpan.FromSeconds(1));

            var createdInstallation = await _hubClient.GetInstallationAsync(installationId);

            Assert.Equal(installation.InstallationId, createdInstallation.InstallationId);
            Assert.Equal(installation.Platform, createdInstallation.Platform);
            Assert.Equal(installation.PushChannel, createdInstallation.PushChannel);
            Assert.Contains(new KeyValuePair<string, string>("var1", "value1"), createdInstallation.PushVariables);
            Assert.Contains("tag1", createdInstallation.Tags);
            Assert.Contains("Template Name", createdInstallation.Templates.Keys);
            Assert.Equal(installation.Templates["Template Name"].Body, createdInstallation.Templates["Template Name"].Body);
            Assert.Equal(installation.Templates["Template Name"].Expiry, createdInstallation.Templates["Template Name"].Expiry);
        }

        [Fact]
        private async Task CreateOrUpdateInstallationAsync_CreateInstallationWithHeadersInTemplate_GetCreatedInstallationWithHeadersInTemplateBack()
        {
            var installationId = Guid.NewGuid().ToString();

            var installation = new Installation
            {
                InstallationId = installationId,
                Platform = NotificationPlatform.Wns,
                PushChannel = _configuration["WindowsDeviceToken"],
                Templates = new Dictionary<string, InstallationTemplate>
                {
                    {
                        "Template Name", new InstallationTemplate
                        {
                            Body = "{\"aps\":{\"alert\":\"alert!\"}}",
                            Headers = new Dictionary<string, string>
                            {
                                {"X-WNS-Type", "wns/toast"}
                            }
                        }
                    }
                }
            };

            await _hubClient.CreateOrUpdateInstallationAsync(installation);

            await Task.Delay(TimeSpan.FromSeconds(1));

            var createdInstallation = await _hubClient.GetInstallationAsync(installationId);

            Assert.Contains("Template Name", createdInstallation.Templates.Keys);
            Assert.Contains(new KeyValuePair<string, string>("X-WNS-Type", "wns/toast"), createdInstallation.Templates["Template Name"].Headers);
        }

        [Fact]
        private async Task PatchInstallationAsync_CreateInstallationWithoutTagsAndAddTagThroughPatch_GetInstallationWithAddedTagBack()
        {
            var installationId = Guid.NewGuid().ToString();

            var installation = new Installation
            {
                InstallationId = installationId,
                Platform = NotificationPlatform.Apns,
                PushChannel = _configuration["AppleDeviceToken"]
            };

            await _hubClient.CreateOrUpdateInstallationAsync(installation);

            await _hubClient.PatchInstallationAsync(installationId, new List<PartialUpdateOperation>
            {
                new PartialUpdateOperation()
                {
                    Operation = UpdateOperationType.Add,
                    Path = "/tags",
                    Value = "tag1"
                }
            });

            await Task.Delay(TimeSpan.FromSeconds(1));

            var updatedInstallation = await _hubClient.GetInstallationAsync(installationId);

            Assert.Contains("tag1", updatedInstallation.Tags);
        }

        [Fact]
        private async Task DeleteInstallationAsync_CreateAndDeleteInstallation_InstallationIsDeleted()
        {
            var installationId = Guid.NewGuid().ToString();

            var installation = new Installation
            {
                InstallationId = installationId,
                Platform = NotificationPlatform.Apns,
                PushChannel = _configuration["AppleDeviceToken"]
            };

            await _hubClient.CreateOrUpdateInstallationAsync(installation);

            await Task.Delay(TimeSpan.FromSeconds(1));

            var createdInstallation = await _hubClient.GetInstallationAsync(installationId);

            await _hubClient.DeleteInstallationAsync(createdInstallation.InstallationId);

            await Task.Delay(TimeSpan.FromSeconds(1));

            await Assert.ThrowsAsync<MessagingEntityNotFoundException>(async () => await _hubClient.GetInstallationAsync(createdInstallation.InstallationId));
        }

        [Fact]
        private async Task SendNotificationAsync_SendAdmNativeNotification_GetSuccessfulResultBack()
        {
            var notification = new AdmNotification("{\"data\":{\"key1\":\"value1\"}}");

            var notificationResult = await _hubClient.SendNotificationAsync(notification, "someRandomTag1 && someRandomTag2");

            Assert.Equal(NotificationOutcomeState.Enqueued, notificationResult.State);
        }

        [Fact]
        private async Task SendNotificationAsync_SendAppleNativeNotification_GetSuccessfulResultBack()
        {
            var notification = new AppleNotification("{\"aps\":{\"alert\":\"alert!\"}}");
            notification.Expiry = DateTime.Now.AddDays(1);
            notification.Priority = 5;

            var notificationResult = await _hubClient.SendNotificationAsync(notification, "someRandomTag1 && someRandomTag2");

            Assert.Equal(NotificationOutcomeState.Enqueued, notificationResult.State);
        }

        [Fact]
        private async Task SendNotificationAsync_SendBaiduNativeNotification_GetSuccessfulResultBack()
        {
            var notification = new BaiduNotification("{\"title\":\"Title\",\"description\":\"Description\"}");
            notification.MessageType = 1;

            var notificationResult = await _hubClient.SendNotificationAsync(notification, "someRandomTag1 && someRandomTag2");

            Assert.Equal(NotificationOutcomeState.Enqueued, notificationResult.State);
        }

        [Fact]
        private async Task SendNotificationAsync_SendGcmNativeNotification_GetSuccessfulResultBack()
        {
            var notification = new GcmNotification("{\"data\":{\"message\":\"Message\"}}");

            var notificationResult = await _hubClient.SendNotificationAsync(notification, "someRandomTag1 && someRandomTag2");

            Assert.Equal(NotificationOutcomeState.Enqueued, notificationResult.State);
        }

        [Fact]
        private async Task SendNotificationAsync_SendMpnsNativeNotification_GetSuccessfulResultBack()
        {
            var notification = new MpnsNotification("<wp:Notification xmlns:wp=\"WPNotification\" Version=\"2.0\"><wp:Tile Id=\"TileId\" Template=\"IconicTile\"><wp:Title Action=\"Clear\">Title</wp:Title></wp:Tile></wp:Notification>");

            var notificationResult = await _hubClient.SendNotificationAsync(notification, "someRandomTag1 && someRandomTag2");

            Assert.Equal(NotificationOutcomeState.Enqueued, notificationResult.State);
        }

        [Fact]
        private async Task SendNotificationAsync_SendWindowsNativeNotification_GetSuccessfulResultBack()
        {
            var notification = new WindowsNotification("<toast><visual><binding template=\"ToastText01\"><text id=\"1\">bodyText</text></binding>  </visual></toast>");

            var notificationResult = await _hubClient.SendNotificationAsync(notification, "someRandomTag1 && someRandomTag2");

            Assert.Equal(NotificationOutcomeState.Enqueued, notificationResult.State);
        }

        [Fact]
        private async Task SendNotificationAsync_SendTemplateNotification_GetSuccessfulResultBack()
        {
            var notification = new TemplateNotification(new Dictionary<string, string>());

            var notificationResult = await _hubClient.SendNotificationAsync(notification, "someRandomTag1 && someRandomTag2");

            Assert.Equal(NotificationOutcomeState.Enqueued, notificationResult.State);
        }

        [Fact]
        private async Task SendDirectNotificationAsync_SendDirectAppleNotification_GetSuccessfulResultBack()
        {
            var notification = new AppleNotification("{\"aps\":{\"alert\":\"alert!\"}}");

            var notificationResult = await _hubClient.SendDirectNotificationAsync(notification, _configuration["AppleDeviceToken"]);

            Assert.Equal(NotificationOutcomeState.Enqueued, notificationResult.State);
        }

        [Fact]
        private async Task SendDirectNotificationAsync_SendDirectAppleBatchNotification_GetSuccessfulResultBack()
        {
            var notification = new AppleNotification("{\"aps\":{\"alert\":\"alert!\"}}");
            notification.Expiry = DateTime.Now.AddDays(1);
            notification.Priority = 5;

            var notificationResult = await _hubClient.SendDirectNotificationAsync(notification, new[] { _configuration["AppleDeviceToken"] });

            Assert.Equal(NotificationOutcomeState.Enqueued, notificationResult.State);
        }

        [Fact]
        private async Task ScheduleNotificationAsync_SendAppleNativeNotification_GetSuccessfulResultBack()
        {
            var notification = new AppleNotification("{\"aps\":{\"alert\":\"alert!\"}}");
            notification.Expiry = DateTime.Now.AddDays(1);
            notification.Priority = 5;

            var notificationResult = await _hubClient.ScheduleNotificationAsync(notification, DateTimeOffset.Now.AddDays(1));

            Assert.NotNull(notificationResult.ScheduledNotificationId);
        }

        [Fact]
        private async Task GetNotificationHubJobAsync_SumbitJob_GetSubmittedJobBack()
        {
            var job = new NotificationHubJob
            {
                JobType = NotificationHubJobType.ExportRegistrations,
                OutputContainerUri = new Uri(_configuration["BlobContainer"])
            };

            var createdJob = await _hubClient.SubmitNotificationHubJobAsync(job);

            createdJob = await _hubClient.GetNotificationHubJobAsync(createdJob.JobId);

            Assert.NotNull(createdJob.JobId);
            Assert.Equal(job.JobType, createdJob.JobType);
            Assert.Equal(job.OutputContainerUri, createdJob.OutputContainerUri);
        }

        [Fact]
        private async Task GetNotificationHubJobsAsync_SumbitJob_GetSubmittedJobBack()
        {
            var job = new NotificationHubJob
            {
                JobType = NotificationHubJobType.ExportRegistrations,
                OutputContainerUri = new Uri(_configuration["BlobContainer"])
            };

            var createdJob = await _hubClient.SubmitNotificationHubJobAsync(job);

            var allJobs = await _hubClient.GetNotificationHubJobsAsync();
            createdJob = allJobs.SingleOrDefault(j => j.JobId == createdJob.JobId);

            Assert.NotNull(createdJob);
            Assert.Equal(job.JobType, createdJob.JobType);
            Assert.Equal(job.OutputContainerUri, createdJob.OutputContainerUri);
        }
    }
}
