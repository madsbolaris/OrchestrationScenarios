// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using Microsoft.Identity.Client.Extensions.Msal;
using Microsoft.Identity.Client;
using Microsoft.Agents.CopilotStudio.Client;

namespace Common
{
    /// <summary>
    /// This sample uses an HttpClientHandler to add an authentication token to the request.
    /// This is used for the interactive authentication flow. 
    /// For more information on how to setup various authentication flows, see the Microsoft Identity documentation at https://aka.ms/msal.
    /// </summary>
    /// <param name="settings">Direct To engine connection settings.</param>
    public class AddTokenHandler(DataverseSettings settings) : DelegatingHandler(new HttpClientHandler())
    {
        private static readonly string _keyChainServiceName = "copilot_studio_client_app";
        private static readonly string _keyChainAccountName = "copilot_studio_client";
        
        private async Task<AuthenticationResult> AuthenticateAsync(CancellationToken ct = default!)
        {
            ArgumentNullException.ThrowIfNull(settings);

            try
            {
                ConnectionSettings connectionSettings = new()
                {
                    Cloud = Microsoft.Agents.CopilotStudio.Client.Discovery.PowerPlatformCloud.Prod,
                    DirectConnectUrl = "",
                    EnvironmentId = settings.EnvironmentId
                };

                string[] scopes = [CopilotClient.ScopeFromSettings(connectionSettings)];

                IPublicClientApplication app = PublicClientApplicationBuilder.Create(settings.ClientId)
                    .WithAuthority(AadAuthorityAudience.AzureAdMyOrg)
                    .WithTenantId(settings.TenantId)
                    .WithRedirectUri("http://localhost")
                    .Build();

                string currentDir = Path.Combine(AppContext.BaseDirectory, "mcs_client_console");
                if (!Directory.Exists(currentDir))
                {
                    Directory.CreateDirectory(currentDir);
                }

                StorageCreationPropertiesBuilder storageProperties = new("TokenCache", currentDir);
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    storageProperties.WithLinuxUnprotectedFile();
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    storageProperties.WithMacKeyChain(_keyChainServiceName, _keyChainAccountName);

                MsalCacheHelper tokenCacheHelper = await MsalCacheHelper.CreateAsync(storageProperties.Build());
                tokenCacheHelper.RegisterCache(app.UserTokenCache);

                IAccount? account = (await app.GetAccountsAsync()).FirstOrDefault();

                try
                {
                    return await app.AcquireTokenSilent(scopes, account).ExecuteAsync(ct);
                }
                catch (MsalUiRequiredException)
                {
                    return await app.AcquireTokenInteractive(scopes).ExecuteAsync(ct);
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Rethrow for upstream handler
                throw;
            }
        }


        /// <summary>
        /// Handles sending the request and adding the token to the request.
        /// </summary>
        /// <param name="request">Request to be sent</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                if (request.Headers.Authorization is null)
                {
                    AuthenticationResult authResponse = await AuthenticateAsync(cancellationToken);
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResponse.AccessToken);
                }

                return await base.SendAsync(request, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Rethrow to propagate cancellation correctly
                throw;
            }
        }

    }
}
