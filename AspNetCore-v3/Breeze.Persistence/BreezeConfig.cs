using Breeze.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Breeze.Persistence {

  public class BreezeConfig {

    
    public static BreezeConfig Instance {
      get {
        lock (__lock) {
          AppDomain.CurrentDomain.AssemblyLoad += CurrentDomain_AssemblyLoad;
          if (__instance == null) {
            var typeCandidates = ProbeAssemblies.SelectMany(a => GetTypes(a));
            var types = typeCandidates.Where(t => typeof (BreezeConfig).IsAssignableFrom(t) && !t.IsAbstract).ToList();

            if (types.Count == 0) {
              __instance = new BreezeConfig();
            } else if (types.Count == 1) {
              __instance = (BreezeConfig) Activator.CreateInstance(types[0]);
            } else {
              throw new Exception(
                "More than one BreezeConfig implementation was found in the currently loaded assemblies - limit is one.");
            }
          }
          return __instance;
        }
      }
    }

    public JsonSerializerSettings GetJsonSerializerSettings() {
      lock (__lock) {
        if (_jsonSerializerSettings == null) {
          _jsonSerializerSettings = CreateJsonSerializerSettings();
        }
        return _jsonSerializerSettings;
      }
    }

    public JsonSerializerSettings GetJsonSerializerSettingsForSave() {
      lock (__lock) {
        if (_jsonSerializerSettingsForSave == null) {
          _jsonSerializerSettingsForSave = CreateJsonSerializerSettingsForSave();
        }
        return _jsonSerializerSettingsForSave;
      }
    }

    public static ReadOnlyCollection<Assembly> ProbeAssemblies {
      get {
        lock (__lock) {
          if (__assemblyCount == 0 || __assemblyCount != __assemblyLoadedCount) {
            // Cache the ProbeAssemblies.
            __probeAssemblies = new ReadOnlyCollection<Assembly>(AppDomain.CurrentDomain.GetAssemblies().Where(a => !IsFrameworkAssembly(a)).ToList());
            __assemblyCount = __assemblyLoadedCount;
          }
          return __probeAssemblies;
        }
      }
    }

    static void CurrentDomain_AssemblyLoad(object sender, AssemblyLoadEventArgs args) {
      Interlocked.Increment(ref __assemblyLoadedCount);
    }
    private static ReadOnlyCollection<Assembly> __probeAssemblies;
    private static int __assemblyCount = 0;
    private static int __assemblyLoadedCount = 0;

    /// <summary>
    /// Override to use a specialized JsonSerializer implementation.
    /// </summary>
    protected virtual JsonSerializerSettings CreateJsonSerializerSettings() {

      var jsonSerializerSettings = new JsonSerializerSettings();
      return JsonSerializationFns.UpdateWithDefaults(jsonSerializerSettings);

    }

    /// <summary>
    /// Override to use a specialized JsonSerializer implementation for saving.
    /// Base implementation uses CreateJsonSerializerSettings() then sets TypeNameHandling to None
    /// </summary>
    protected virtual JsonSerializerSettings CreateJsonSerializerSettingsForSave() {
      var settings = CreateJsonSerializerSettings();
      settings.TypeNameHandling = TypeNameHandling.None;
      return settings;
    }

    public static bool IsFrameworkAssembly(Assembly assembly) {
      var fullName = assembly.FullName;
      if (fullName.StartsWith("Microsoft.")) return true;
      if (fullName.StartsWith("EntityFramework")) return true;
      if (fullName.StartsWith("NHibernate")) return true;
      var attrs = assembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false).OfType<AssemblyProductAttribute>();
      var attr = attrs.FirstOrDefault();
      if (attr == null) {
        return false;
      }
      var productName = attr.Product;
      return FrameworkProductNames.Any(nm => productName.StartsWith(nm));
    }

    protected static IEnumerable<Type> GetTypes(Assembly assembly) {

      try {
        return assembly.GetTypes();
      } catch (Exception ex) {
        string msg = string.Empty;
        if (ex is System.Reflection.ReflectionTypeLoadException) {
          msg = ((ReflectionTypeLoadException)ex).LoaderExceptions.ToAggregateString(". ");
        }
        Trace.WriteLine("Breeze probing: Unable to execute Assembly.GetTypes() for "
          + assembly.ToString() + "." + msg);

        return new Type[] { };
      }
    }

    protected static readonly List<String> FrameworkProductNames = new List<String> {
      "Microsoft®",
      "Microsoft (R)",
      "Microsoft ASP.",
      "System.Net.Http",
      "Json.NET",
      "Antlr3.Runtime",
      "Iesi.Collections",
      "WebGrease",
      "Breeze.ContextProvider",
      "Breeze.Persistence",
      "Breeze.Core",
      "Breeze.AspNetCore"
    };

    /// <summary>
    /// Returns TransactionSettings.Default.  Override to return different settings.
    /// </summary>
    /// <returns></returns>
    public virtual TransactionSettings GetTransactionSettings()
    {
        return TransactionSettings.Default;
    }

    private static Object __lock = new Object();
    private static BreezeConfig __instance;
    
    private JsonSerializerSettings _jsonSerializerSettings = null;
    private JsonSerializerSettings _jsonSerializerSettingsForSave = null;

  }


}
