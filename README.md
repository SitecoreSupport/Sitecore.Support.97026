# Sitecore.Support.97026
Experimental support for Azure database geo-replication feature on Sitecore CD instances.
## Credits
The original code was developed by [andrew-at-sitecore](https://github.com/andrew-at-sitecore).
## Main
This repository contains Sitecore patch 97026 that introduces basic support for Azure database geo-replication feature.   
Database geo-replication feature requires secondary database to be read-only. Sitecore application keeps track of the events using EventQueue stamps that get written to the Properties table. In order to allow database to be read only, the patch persists properties into the local drive instead of the database.
> Note: solutions hosted in Azure PaaS need to persist the properties into a blob rather than local file system.

## Deployment
To apply the patch on CD instances working with read-only web database follow these steps:  
1. Place the `Sitecore.Support.97026.dll` assembly into the `\bin` folder.  
2. Place the `Sitecore.Support.97026.config` file into the `\App_Config\Include` folder.  
3. *[Optional]* Use `Sitecore.Support.97026.MainDataProvider.config.example` file to persist properties to the file system for all Sitecore databases.  

> Note: this solution does not support proxy items. The configuration of the patch disables proxy items for `web` database.

## Content
The patch includes these files:  
1. `\bin\Sitecore.Support.97026.dll`  
2. `\App_Config\Include\Sitecore.Support.97026.config`  
3. `\App_Config\Include\Sitecore.Support.97026.MainDataProvider.config.example`
