# OakGov.Etl.ZumoDestination
Microsoft SQL Server Integrations Services custom destination for Azure Mobile Service

## Technologies
This is a custom destination component for [Microsoft® SQL Server Integration Services 2012](https://www.microsoft.com/en-us/download/details.aspx?id=36843). It was developed using Microsoft Visual Studio 2012.

It connects to a Microsoft Azure [App Service - Mobile App](https://docs.microsoft.com/en-us/azure/app-service-mobile/) and synchronizes the data in the Azure Mobile App Service based on the incoming data from the inputs.

## Example
The result is a cloud hosted data service that feeds the [Inmate Locator app](https://github.com/knaopel/inmate-locator) located at: [Oakland County Sheriff's Inmate Locator](https://www.oakgov.com/sheriff/Corrections-Courts/Inmate-Locator/) that is synchronized with in-house data.