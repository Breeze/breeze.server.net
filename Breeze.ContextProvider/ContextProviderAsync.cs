using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Transactions;
using System.Xml.Linq;

namespace Breeze.ContextProvider {
  // Base for ContextProvide and ContextProvideAsync
  public abstract class ContextProviderAsync: ContextProvider {


	  #region async

	  public async Task<SaveResult> SaveChangesAsync(JObject saveBundle, TransactionSettings transactionSettings = null)
	  {
		  HandleSaveBundle(saveBundle);



		  transactionSettings = transactionSettings ?? BreezeConfig.Instance.GetTransactionSettings();
		  try
		  {
			  if (transactionSettings.TransactionType == TransactionType.TransactionScope)
			  {
				  var txOptions = transactionSettings.ToTransactionOptions();
				  using (var txScope = new TransactionScope(TransactionScopeOption.Required, txOptions, System.Transactions.TransactionScopeAsyncFlowOption.Enabled))
				  {
					  await OpenAndSaveAsync(SaveWorkState);
					  txScope.Complete();
				  }
			  }
			  else if (transactionSettings.TransactionType == TransactionType.DbTransaction)
			  {
				  throw new Exception("No async save implemented with DbTransaction");
			  }
			  else
			  {
				  await OpenAndSaveAsync(SaveWorkState);
			  }
		  }
		  catch (EntityErrorsException e)
		  {
			  SaveWorkState.EntityErrors = e.EntityErrors;
			  throw;
		  }
		  catch (Exception e2)
		  {
			  if (!HandleSaveException(e2, SaveWorkState))
			  {
				  throw;
			  }
		  }
		  finally
		  {
			  CloseDbConnection();
		  }

		  return SaveWorkState.ToSaveResult();

	  }


	  private async Task OpenAndSaveAsync(SaveWorkState saveWorkState)
	  {

		  await OpenDbConnectionAsync();    // ensure connection is available for BeforeSaveEntities
		  await saveWorkState.BeforeSaveAsync();
		  await SaveChangesCoreAsync(saveWorkState);
		  saveWorkState.AfterSave();
	  }


	  protected abstract Task SaveChangesCoreAsync(SaveWorkState saveWorkState);

	  /// <summary>
	  /// Internal use only.  Should only be called by ContextProvider during SaveChanges.
	  /// Opens the DbConnection used by the ContextProvider's implementation.
	  /// Method must be idempotent; after it is called the first time, subsequent calls have no effect.
	  /// </summary>
	  protected abstract Task OpenDbConnectionAsync();



	  #endregion




  }

}