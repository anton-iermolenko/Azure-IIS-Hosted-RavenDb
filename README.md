Sample: Azure hosting of RavenDb as Web role
============================================

This is a sample of how you can host RavenDb as Web role. It is highly recommended to use RavenDB build >602 (which is currently unstable). Version embedded here is 573.

A few things to mention
-----------------------
* Tested to be working propertly in "Esent" storage mode when running in *real* Azure (with build >602).
* Application pool is configured to never recycle and timeout to achieve best performance
* Max query string is configured for up to 16Kb
* If you need to host it as Worker Role search "simplefx/AzureRavenDB" on GitHub. Originally I was using that solution to get it working for me.