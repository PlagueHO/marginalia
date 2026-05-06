// Entra ID App Registrations for Marginalia
// Uses the Microsoft Graph Bicep extension to create API and SPA app registrations.

extension microsoftGraphV1

@description('Environment name used as display name prefix.')
param environmentName string

@description('Production redirect URI for the SPA (e.g., https://<staticwebapp>.azurestaticapps.net).')
param spaProductionRedirectUri string = ''

// Deterministic GUID for the access_as_user scope
var accessAsUserScopeId = guid('marginalia-access-as-user')

// ---- API App Registration ----
resource apiApp 'Microsoft.Graph/applications@v1.0' = {
  uniqueName: 'marginalia-api'
  displayName: '${environmentName}-marginalia-api'
  signInAudience: 'AzureADMyOrg'
  api: {
    requestedAccessTokenVersion: 2
    oauth2PermissionScopes: [
      {
        id: accessAsUserScopeId
        value: 'access_as_user'
        type: 'User'
        adminConsentDisplayName: 'Access Marginalia API'
        adminConsentDescription: 'Allow the app to access Marginalia API on behalf of the signed-in user.'
        userConsentDisplayName: 'Access Marginalia API'
        userConsentDescription: 'Allow the app to access Marginalia API on your behalf.'
        isEnabled: true
      }
    ]
  }
  optionalClaims: {
    accessToken: [
      {
        name: 'idtyp'
        essential: false
      }
    ]
  }
  identifierUris: [
    'api://marginalia-api'
  ]
}

resource apiServicePrincipal 'Microsoft.Graph/servicePrincipals@v1.0' = {
  appId: apiApp.appId
}

// ---- SPA App Registration ----

var spaRedirectUris = empty(spaProductionRedirectUri)
  ? [
      'http://localhost:5173'
    ]
  : [
      'http://localhost:5173'
      spaProductionRedirectUri
    ]

resource spaApp 'Microsoft.Graph/applications@v1.0' = {
  uniqueName: 'marginalia-spa'
  displayName: '${environmentName}-marginalia-spa'
  signInAudience: 'AzureADMyOrg'
  spa: {
    redirectUris: spaRedirectUris
  }
  requiredResourceAccess: [
    {
      resourceAppId: apiApp.appId
      resourceAccess: [
        {
          id: accessAsUserScopeId
          type: 'Scope'
        }
      ]
    }
  ]
}

resource spaServicePrincipal 'Microsoft.Graph/servicePrincipals@v1.0' = {
  appId: spaApp.appId
}

// ---- Pre-authorize SPA for API scope (avoids user consent prompt) ----
// NOTE: This may require a separate update if circular reference doesn't resolve in single pass.
// If deployment fails here, move preAuthorizedApplications to a post-deployment script.

// ---- Outputs ----

@description('The client ID (appId) of the API app registration.')
output apiClientId string = apiApp.appId

@description('The client ID (appId) of the SPA app registration.')
output spaClientId string = spaApp.appId
