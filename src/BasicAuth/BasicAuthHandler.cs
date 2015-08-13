﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Authentication;
using Microsoft.AspNet.Authentication.Notifications;
using Microsoft.AspNet.Http.Authentication;
using Microsoft.AspNet.Http.Features.Authentication;

namespace edjCase.BasicAuth
{
	/// <summary>
	/// Basic auth handler that handles authentication and unauthorized requests
	/// </summary>
	internal class BasicAuthHandler : AuthenticationHandler<BasicAuthOptions>
	{
		/// <summary>
		/// Handles authentication for Basic auth requests
		/// </summary>
		/// <returns>Task that results in an authentication ticket for credential or null for unauthorized request</returns>
		protected async override Task<AuthenticationTicket> HandleAuthenticateAsync()
		{
			string[] authHeaderValues;
			bool hasAuthHeader = this.Request.Headers.TryGetValue(BasicAuthConstants.AuthHeaderName, out authHeaderValues) &&
								 authHeaderValues.Any();

			if (!hasAuthHeader)
			{
				return null; //TODO?
			}
			string basicAuthValue = authHeaderValues.First();
			bool headerIsBasicAuth = basicAuthValue.StartsWith("Basic ");
			if (!headerIsBasicAuth)
			{
				return null; //TODO?
			}
			try
			{
				BasicAuthCredential credential = BasicAuthCredential.Parse(basicAuthValue);

				AuthenticationProperties authProperties = new AuthenticationProperties();

				BasicAuthInfo authInfo = new BasicAuthInfo(credential, authProperties, this.Options);

				if(this.Options.AuthenticateCredential == null)
				{
					throw new BasicAuthConfigurationException("AuthenticateCredential method was not set in the configuration");
				}

				AuthenticationTicket ticket = await this.Options.AuthenticateCredential(authInfo);
				return ticket;
			}
			catch (Exception ex)
			{
				var failedNotification = new AuthenticationFailedNotification<string, BasicAuthOptions>(this.Context, this.Options)
				{
					ProtocolMessage = "", //TODO
					Exception = ex
				};

				if (this.Options.OnException != null)
				{
					await this.Options.OnException(failedNotification);
				}

				if (failedNotification.HandledResponse)
				{
					return failedNotification.AuthenticationTicket;
				}

				if (failedNotification.Skipped)
				{
					return null;
				}

				throw;
			}
		}

		/// <summary>
		/// Handles unauthorized Basic auth requests
		/// </summary>
		/// <param name="context"></param>
		/// <returns>True if response is handled</returns>
		protected override Task<bool> HandleUnauthorizedAsync(ChallengeContext context)
		{
			this.Response.Headers.AppendValues("WWW-Authenticate", $"Basic realm=\"{this.Options.Realm}\"");
			this.Response.StatusCode = 401; //Unauthorized
			return Task.FromResult(true);
		}
	}
}
