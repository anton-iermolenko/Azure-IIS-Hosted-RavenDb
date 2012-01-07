Sample: Azure hosting of RavenDb as Web role
============================================

This is a sample of how you can host RavenDb as Web role.

A few things to mention
-----------------------
* Tested to be working propertly in "Esent" storage mode when running in *real* Azure. In emulator it automatically switches to Munin.
* Application pool is configured to never recycle and timeout to achieve best performance
* Max query string is configured for up to 16Kb
* If you need to host it as Worker Role search "simplefx/AzureRavenDB" on GitHub. Originally I was using that solution to get it working for me.