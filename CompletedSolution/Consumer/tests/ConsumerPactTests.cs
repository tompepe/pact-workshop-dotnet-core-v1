using System;
using Xunit;
using PactNet.Mocks.MockHttpService;
using PactNet.Mocks.MockHttpService.Models;
using Consumer;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using GraphQL.Client.Http;
using GraphQL;
using GraphQL.Client.Serializer.Newtonsoft;
using System.Text.RegularExpressions;

namespace tests
{
    public class ConsumerPactTests : IClassFixture<ConsumerPactClassFixture>
    {
        private IMockProviderService _mockProviderService;
        private string _mockProviderServiceBaseUri;

        public ConsumerPactTests(ConsumerPactClassFixture fixture)
        {
            _mockProviderService = fixture.MockProviderService;
            _mockProviderService.ClearInteractions(); //NOTE: Clears any previously registered interactions before the test is run
            _mockProviderServiceBaseUri = fixture.MockProviderServiceBaseUri;
        }

        // [Fact(Skip="this is for step 2")]
        [Fact]
        public async void ItHandlesDemoFirstGraphqlQuery()
        {
            // Arange
            var myJsonString = File.ReadAllText("../../../../../consumer-provider2.json");
            var myJObject = JObject.Parse(myJsonString);
            var scenario = myJObject.SelectToken(".interactions[?(@.description=='graphql - SrcsetImages just the big ones')]");
            var requestTemplate = scenario.SelectToken(".request");
            var responseTemplate = scenario.SelectToken(".response");

            var request = requestTemplate.ToObject<ProviderServiceRequest>();
            var response = responseTemplate.ToObject<ProviderServiceResponse>();
            request.Headers = new Dictionary<string, object>
                                    {
                                        { "Content-Type", "application/json; charset=utf-8" }
                                    };

            _mockProviderService.Given("There is data")
                                .UponReceiving("testing demo first")
                                .With(request)
                                .WillRespondWith(response);

            // Act
            using (var client = new HttpClient { BaseAddress = new Uri(_mockProviderServiceBaseUri) })
            {
                var req = $"{request.Body}";
                var x = await client.PostAsync($"/api/", new StringContent(req, Encoding.UTF8, "application/json"));
                var resultBodyText = x.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                // Assert... todo actually inject the HttpClient into your application, call the POSTAsync in your application code, and verify that your application actually does something useful with the mock request / response
                Assert.Equal(JObject.Parse($"{response.Body}"), JObject.Parse(resultBodyText));
            }
        }

        [Fact]
        public async void ItHandlesDemoFirstGraphqlQueryWithGraphQLRequest()
        {
            // Arange
            var myJsonString = File.ReadAllText("../../../../../consumer-provider2.json");
            var myJObject = JObject.Parse(myJsonString);
            var scenario = myJObject.SelectToken(".interactions[?(@.description=='graphql - SrcsetImages just the big ones')]");
            var requestTemplate = scenario.SelectToken(".request");
            var responseTemplate = scenario.SelectToken(".response");

            var request = requestTemplate.ToObject<ProviderServiceRequest>();
            var response = responseTemplate.ToObject<ProviderServiceResponse>();
            var expectedResponseData = responseTemplate.SelectToken(".body.data.site").ToObject<SiteType>();
            request.Headers = new Dictionary<string, object>
                                    {
                                        { "Content-Type", "application/json" }
                                    };

            _mockProviderService.Given("There is data")
                                .UponReceiving("testing demo first then using graphql")
                                .With(request)
                                .WillRespondWith(response);

            // Act
            using (var graphQLClient = new GraphQLHttpClient(_mockProviderServiceBaseUri, new NewtonsoftJsonSerializer())) //still does not work
            // using (var graphQLClient = new GraphQLHttpClient("http://localhost:58100/api/", new NewtonsoftJsonSerializer())) //works but only with the regex... I think the regex matchers are dropped off when importing via newtonsoft Json Desearialization
            {
                var imagesRequest = new GraphQLRequest
                {
                    Query = Regex.Replace(@"
                        query SrcsetImages(
                            $id: Int!
                            $width960: Int!
                            $width128: Int!
                        ) {
                            site {
                                product(entityId: $id) {
                                    images {
                                        edges {
                                            node {
                                                url960wide: url(width: $width960)
                                                url1280wide: url(width: $width128)
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    ", @"\s\s+", ""),
                    OperationName = "SrcsetImages",
                    Variables = new
                    {
                        id = 66,
                        width960 = 960,
                        width128 = 128
                    }
                };

                var graphQLResponse = await graphQLClient.SendQueryAsync<SiteDataType>(imagesRequest);
                
                // Assert... todo actually inject the HttpClient into your application, call the POSTAsync in your application code, and verify that your application actually does something useful with the mock request / response                
                Assert.Equal(JsonConvert.SerializeObject(expectedResponseData), JsonConvert.SerializeObject(graphQLResponse.Data.Site));
            }
        }

        // [Fact(Skip="this is for step 2")]
        [Fact]
        public void ItHandlesInvalidDateParam()
        {
            // Arange
            var invalidRequestMessage = "validDateTime is not a date or time";
            _mockProviderService.Given("There is data")
                                .UponReceiving("A invalid GET request for Date Validation with invalid date parameter")
                                .With(new ProviderServiceRequest
                                {
                                    Method = HttpVerb.Get,
                                    Path = "/api/provider",
                                    Query = "validDateTime=lolz"
                                })
                                .WillRespondWith(new ProviderServiceResponse
                                {
                                    Status = 400,
                                    Headers = new Dictionary<string, object>
                                    {
                                        { "Content-Type", "application/json; charset=utf-8" }
                                    },
                                    Body = new
                                    {
                                        message = invalidRequestMessage
                                    }
                                });

            // Act
            var result = ConsumerApiClient.ValidateDateTimeUsingProviderApi("lolz", _mockProviderServiceBaseUri).GetAwaiter().GetResult();
            var resultBodyText = result.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            // Assert
            Assert.Contains(invalidRequestMessage, resultBodyText);
        }

        [Fact]
        public void ItHandlesEmptyDateParam()
        {
            // Arrange
            var invalidRequestMessage = "validDateTime is required";
            _mockProviderService.Given("There is data")
                                .UponReceiving("A invalid GET request for Date Validation with empty string date parameter")
                                .With(new ProviderServiceRequest
                                {
                                    Method = HttpVerb.Get,
                                    Path = "/api/provider",
                                    Query = "validDateTime="
                                })
                                .WillRespondWith(new ProviderServiceResponse
                                {
                                    Status = 400,
                                    Headers = new Dictionary<string, object>
                                    {
                                        { "Content-Type", "application/json; charset=utf-8" }
                                    },
                                    Body = new
                                    {
                                        message = invalidRequestMessage
                                    }
                                });

            // Act
            var result = ConsumerApiClient.ValidateDateTimeUsingProviderApi(String.Empty, _mockProviderServiceBaseUri).GetAwaiter().GetResult();
            var resultBodyText = result.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            // Assert
            Assert.Contains(invalidRequestMessage, resultBodyText);
        }

        [Fact]
        public void ItHandlesNoData()
        {
            // Arrange
            _mockProviderService.Given("There is no data")
                                .UponReceiving("A valid GET request for Date Validation")
                                .With(new ProviderServiceRequest
                                {
                                    Method = HttpVerb.Get,
                                    Path = "/api/provider",
                                    Query = "validDateTime=04/04/2018"
                                })
                                .WillRespondWith(new ProviderServiceResponse
                                {
                                    Status = 404
                                });

            // Act
            var result = ConsumerApiClient.ValidateDateTimeUsingProviderApi("04/04/2018", _mockProviderServiceBaseUri).GetAwaiter().GetResult();
            var resultStatus = (int)result.StatusCode;

            // Assert
            Assert.Equal(404, resultStatus);
        }

        [Fact]
        public void ItParsesADateCorrectly()
        {
            var expectedDateString = "04/05/2018";
            var expectedDateParsed = DateTime.Parse(expectedDateString).ToString("dd-MM-yyyy HH:mm:ss");

            // Arrange
            _mockProviderService.Given("There is data")
                                .UponReceiving("A valid GET request for Date Validation")
                                .With(new ProviderServiceRequest
                                {
                                    Method = HttpVerb.Get,
                                    Path = "/api/provider",
                                    Query = $"validDateTime={expectedDateString}"
                                })
                                .WillRespondWith(new ProviderServiceResponse
                                {
                                    Status = 200,
                                    Headers = new Dictionary<string, object>
                                    {
                                        { "Content-Type", "application/json; charset=utf-8" }
                                    },
                                    Body = new
                                    {
                                        test = "NO",
                                        validDateTime = expectedDateParsed
                                    }
                                });

            // Act
            var result = ConsumerApiClient.ValidateDateTimeUsingProviderApi(expectedDateString, _mockProviderServiceBaseUri).GetAwaiter().GetResult();
            var resultBody = result.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            // Assert
            Assert.Contains(expectedDateParsed, resultBody);
        }
    }


    public class SiteDataType 
    {
        [JsonProperty(PropertyName = "site")]
        public SiteType Site { get; set; }
    }
    public class SiteType
    {
        [JsonProperty(PropertyName = "product")]
        public ProductType Product { get; set; }
    }

    public class ProductType
    {

        [JsonProperty(PropertyName = "images")]
        public ImagesType Images { get; set; }
    }

    public class ImagesType
    {

        [JsonProperty(PropertyName = "edges")]
        public EdgeType[] Edges { get; set; }
    }

    public class EdgeType
    {

        [JsonProperty(PropertyName = "node")]
        public NodeType Node { get; set; }
    }

    public class NodeType
    {
        [JsonProperty(PropertyName = "url960wide")]
        public string Url960Wide { get; set; }
        [JsonProperty(PropertyName = "url1280wide")]
        public string Url1280Wide { get; set; }
    }
}