﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace RegistryEx.Test.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("RegistryEx.Test.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Windows Registry Editor Version 5.00
        ///
        ///[HKEY_CURRENT_USER\_RH_Test_]
        ///&quot;Test&quot;=foobar:00000123
        ///
        ///.
        /// </summary>
        internal static string InvalidKind {
            get {
                return ResourceManager.GetString("InvalidKind", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Windows Registry Editor Version 5.00
        ///
        ///[HKEY_CURRENT_USER\_RH_Test_]
        ///@=&quot;文字文字&quot;
        ///&quot;Binary&quot;=hex:fa,51,6f,89
        ///&quot;Dword&quot;=dword:00000123
        ///&quot;None&quot;=hex(0):19,89,06,04,00
        ///&quot;Qword&quot;=hex(b):88,68,66,00,00,00,00,00
        ///&quot;Multi&quot;=hex(7):53,00,74,00,72,00,30,00,00,00,53,00,74,00,72,00,31,00,00,00,00,\
        ///  00
        ///&quot;Expand&quot;=hex(2):25,00,55,00,53,00,45,00,52,00,50,00,52,00,4f,00,46,00,49,00,4c,\
        ///  00,45,00,25,00,00,00
        ///
        ///.
        /// </summary>
        internal static string Kinds {
            get {
                return ResourceManager.GetString("Kinds", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Windows Registry Editor Version 5.00
        ///
        ///&quot;StringValue&quot;=&quot;中文内容&quot;
        ///
        ///.
        /// </summary>
        internal static string NoKey {
            get {
                return ResourceManager.GetString("NoKey", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Windows Registry Editor Version 5.00
        ///
        ///[HKEY_CURRENT_USER\_RH_Test_]
        ///&quot;foo&quot;=&quot;bar&quot;
        ///
        ///[HKEY_CURRENT_USER\_RH_Test_\123456]
        ///
        ///[HKEY_CURRENT_USER\_RH_Test_\Sub]
        ///
        ///;Above keys has been cleared
        ///[-HKEY_CURRENT_USER\_RH_Test_]
        ///
        ///[HKEY_CURRENT_USER\_RH_Test_\Sub]
        ///@=dword:00000044
        ///
        ///[HKEY_CURRENT_USER\_RH_Test_]
        ///&quot;foo&quot;=-
        ///@=&quot;Invalid&quot;
        ///
        ///;Above values has been overridden
        ///&quot;foo&quot;=&quot;baz&quot;
        ///@=dword:00000123
        ///.
        /// </summary>
        internal static string Redundant_0 {
            get {
                return ResourceManager.GetString("Redundant_0", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Windows Registry Editor Version 5.00
        ///
        ///[-HKEY_CURRENT_USER\_RH_Test_\Sub]
        ///
        ///[-HKEY_CURRENT_USER\_RH_Test_\Abother]
        ///
        ///[-HKEY_CURRENT_USER\_RH_Test_]
        ///
        ///
        ///[HKEY_CURRENT_USER\_RH_Test_\Abother]
        ///
        ///[HKEY_CURRENT_USER\_RH_Test_\Sub]
        ///@=dword:00000044
        ///
        ///[HKEY_CURRENT_USER\_RH_Test_]
        ///
        ///[HKEY_CURRENT_USER\_RH_Test_\Sub\Deep]
        ///
        ///.
        /// </summary>
        internal static string Redundant_1 {
            get {
                return ResourceManager.GetString("Redundant_1", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Windows Registry Editor Version 5.00
        ///
        ///[-HKEY_CURRENT_USER\_RH_Test_]
        ///
        ///[HKEY_CURRENT_USER\_RH_Test_]
        ///&quot;foo&quot;=&quot;baz&quot;
        ///@=dword:00000123
        ///
        ///[HKEY_CURRENT_USER\_RH_Test_\Sub]
        ///@=dword:00000044
        ///
        ///.
        /// </summary>
        internal static string SubKey {
            get {
                return ResourceManager.GetString("SubKey", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Windows Registry Editor Version 5.00
        ///
        ///[HKEY_CURRENT_USER\_RH_Test_]
        ///&quot;Multi&quot;=hex(7):53,00,74,00,\
        ///  72,00,30,00,00\
        ///  
        ///.
        /// </summary>
        internal static string TruncatedValue {
            get {
                return ResourceManager.GetString("TruncatedValue", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Windows Registry Editor Version 5.00
        ///
        ///[HKEY_CURRENT_USER\_RH_Test_]
        ///&quot;Multi&quot;=hex(7):53,00,74,00,\ ;Comment at end
        ///  72,00,30,00,\
        ///  ;Comment line inside the value
        ///  00,00,53,00,74,00,72,00,31,00,00,00,00,\
        ///  00
        ///
        ///.
        /// </summary>
        internal static string ValueParts {
            get {
                return ResourceManager.GetString("ValueParts", resourceCulture);
            }
        }
    }
}
