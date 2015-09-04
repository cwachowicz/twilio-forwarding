using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Web.Http;
using Twilio;
using Twilio.TwiML;
using Twilio.TwiML.Mvc;
using RestSharp;
using Newtonsoft.Json;
using Support_Forwarding.Models;

namespace Support_Forwarding.Controllers
{
    public class ResponseController : ApiController
    {
        public HttpResponseMessage Get()
        {
            
            var client = new RestClient() { Timeout = 10000, BaseUrl = new System.Uri("https://<subdomain>.pagerduty.com/api/") };
            client.AddDefaultParameter(new Parameter { Name = "Authorization", Value = "Token token=<token>", Type = ParameterType.HttpHeader });

            var onCallRequest = new RestRequest("v1/escalation_policies/on_call", Method.GET);

            IRestResponse onCallResponse = client.Execute(onCallRequest);
            var content = onCallResponse.Content;
            dynamic pagerDutyOnCall = JsonConvert.DeserializeObject(content);
            dynamic user = pagerDutyOnCall.escalation_policies[0].escalation_rules[0].rule_object;

            User userObject = new Models.User();
            userObject.id = user.id;
            userObject.name = user.name;
            userObject.email = user.email;

            var userRequest = new RestRequest("v1/users/" + userObject.id + "/contact_methods", Method.GET);

            IRestResponse userResponse = client.Execute(userRequest);
            var userDetails = userResponse.Content;
            dynamic userMobile = JsonConvert.DeserializeObject(userDetails);
            var contactCounts = userMobile.contact_methods.Count;
            for(int i = 0; i < contactCounts; i++)
            {
                if(userMobile.contact_methods[i].type == "phone")
                {
                    userObject.mobile = "+" + userMobile.contact_methods[i].country_code + userMobile.contact_methods[i].address;
                }
            }


            var twilioResponse = new TwilioResponse();
            twilioResponse.Say("Welcome to the Support Centre. Please wait while we route your call.");
            twilioResponse.Dial(userObject.mobile);
            

            return this.Request.CreateResponse(HttpStatusCode.OK, twilioResponse.Element, new XmlMediaTypeFormatter());
        }
    }
}
