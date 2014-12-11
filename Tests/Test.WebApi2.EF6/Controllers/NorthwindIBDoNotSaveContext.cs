using System;
using System.Collections.Generic;
using Breeze.ContextProvider;
using Breeze.ContextProvider.EF6;
using Models.NorthwindIB.CF;
using Newtonsoft.Json.Linq;

namespace Sample_WebApi2.Controllers
{
  /// <summary>
  /// A context whose SaveChanges method does not save
  /// but it will prepare its <see cref="SaveWorkState"/> (with SaveMap)
  /// so developers can do what they please with the same information.
  /// See the <see cref="GetSaveMapFromSaveBundle"/> method;
  /// </summary>
  public class NorthwindIBDoNotSaveContext : EFContextProvider<NorthwindIBContext_CF>
  {
    /// <summary>
    /// Open whatever is the "connection" to the "database" where you store entity data.
    /// This implementation does nothing.
    /// </summary>
    protected override void OpenDbConnection(){}

    /// <summary>
    /// Perform your custom save to wherever you store entity data.
    /// This implementation does nothing.
    /// </summary>
    protected override void SaveChangesCore(SaveWorkState saveWorkState) {}

    /// <summary>
    /// Return the SaveMap that Breeze prepares
    /// while performing <see cref="ContextProvider.SaveChanges"/>.
    /// </summary>
    /// <remarks>
    /// Calls SaveChanges which internally creates a <see cref="SaveWorkState"/>
    /// from the <see param="saveBundle"/> and then runs the BeforeSave and AfterSave logic (if any).
    /// <para>
    /// While this works, it is hacky if all you want is the SaveMap.
    /// The real purpose of this context is to demonstrate how to
    /// pare down a ContextProvider, benefit from the breeze save pre/post processing,
    /// and then do your own save inside the <see cref="SaveChangesCore"/>.
    /// </para>
    /// </remarks>
    /// <returns>
    /// Returns the <see cref="SaveWorkState.SaveMap"/>.
    /// </returns>
    public Dictionary<Type, List<EntityInfo>> GetSaveMapFromSaveBundle(JObject saveBundle)
    {
      SaveChanges(saveBundle); // creates the SaveWorkState and SaveMap as a side-effect
      return SaveWorkState.SaveMap;
    }
  }
}