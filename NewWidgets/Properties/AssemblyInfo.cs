using System.Runtime.CompilerServices;
#if OLD
using System.Reflection;

// Information about this assembly is defined by the following attributes. 
// Change them to the values specific to your project.

[assembly: AssemblyTitle("New Widgets")]
[assembly: AssemblyDescription("UI Widgets Library")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("RunServer")]
[assembly: AssemblyProduct("New Widgets")]
[assembly: AssemblyCopyright("RunServer, 2016-2020")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// The assembly version has the format "{Major}.{Minor}.{Build}.{Revision}".
// The form "{Major}.{Minor}.*" will automatically update the build and revision,
// and "{Major}.{Minor}.{Build}.*" will update just the revision.

[assembly: AssemblyVersion("1.6.1.*")]

// The following attributes are used to specify the signing key for the assembly, 
// if desired. See the Mono documentation for more information about signing.

//[assembly: AssemblyDelaySign(false)]
//[assembly: AssemblyKeyFile("")]
#endif

#if __IOS__
[assembly: InternalsVisibleTo("RunMobile.OpenTK.iOS")]
#elif __ANDROID__
[assembly: InternalsVisibleTo("RunMobile.OpenTK.Android")]
#else
[assembly: InternalsVisibleTo("RunMobile.OpenTK")]
[assembly: InternalsVisibleTo("RunMobile.OpenTK.ES")]
#endif
