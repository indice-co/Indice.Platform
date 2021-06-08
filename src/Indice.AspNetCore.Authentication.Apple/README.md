# AddAppleID

This is an attempt to make an Apple ID compatible Authentication Provider like the ones provided by Microsoft under the namespace Microsoft.AspNetCore.Authentication.*

Influenced by the excelent article by Scott Brady found here https://www.scottbrady91.com/OpenID-Connect/Implementing-Sign-In-with-Apple-in-ASPNET-Core

ECDsa and privateKey loading needs userprofile loaded on a server machine (azure) 
In azure website this can be accomplished with the setting `WEBSITE_LOAD_USER_PROFILE=1`