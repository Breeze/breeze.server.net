using Breeze.Persistence;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Transactions;

namespace TestBreeze {
  [TestClass]
  public sealed class TestPersistence {

    [TestMethod]
    public void SaveChangesAsync_TransactionScope_PreservesAmbientTransactionAndCommitsAfterAwait() {
      var manager = new ProbeManager();
      var saveBundle = JObject.Parse("{\"entities\":[],\"saveOptions\":{}}");
      var settings = new TransactionSettings { TransactionType = TransactionType.TransactionScope };
      SaveResult? result = null;
      Exception? saveException = null;
      TransactionStatus? status = null;

      // A failed scope disposal must not leave ambient state on a reusable test thread.
      var caller = new Thread(() => {
        try {
          try {
            var save = manager.SaveChangesAsync(saveBundle, settings);
            var suspended = !save.IsCompleted;
            // Release only after SaveChangesAsync has awaited the incomplete connection task.
            manager.ConnectionOpened.SetResult(true);
            result = save.GetAwaiter().GetResult();
            Assert.IsTrue(suspended, "the save should suspend while opening the connection");
          } finally {
            using var tx = manager.TransactionClone;
            if (tx != null) {
              status = tx.TransactionInformation.Status;
            }
          }
        } catch (Exception e) {
          saveException = e;
        }
      }) { IsBackground = true };

      using (ExecutionContext.SuppressFlow()) {
        caller.Start();
      }
      Assert.IsTrue(caller.Join(TimeSpan.FromSeconds(10)), "the save should finish within 10 seconds");
      Assert.IsNull(saveException, $"the save should not throw: {saveException}");
      Assert.IsNotNull(result);
      Assert.AreNotEqual(manager.OpenThreadId, manager.CoreThreadId, "the save should resume on another thread");
      Assert.IsNotNull(manager.OpenTransactionId, "an ambient transaction should exist when the connection is opened");
      Assert.AreEqual(manager.OpenTransactionId, manager.CoreTransactionId, "the same transaction should be current during the save");
      Assert.AreEqual(TransactionStatus.Committed, status, "the transaction should commit before the save returns");
    }

    // An empty save bundle and a gated connection task exercise the pipeline without a database.
    private sealed class ProbeManager : PersistenceManager {
      public TaskCompletionSource<bool> ConnectionOpened { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
      public int OpenThreadId { get; private set; }
      public int CoreThreadId { get; private set; }
      public string? OpenTransactionId { get; private set; }
      public string? CoreTransactionId { get; private set; }
      public Transaction? TransactionClone { get; private set; }

      protected override Task OpenDbConnectionAsync(CancellationToken cancellationToken) {
        OpenThreadId = Environment.CurrentManagedThreadId;
        OpenTransactionId = Transaction.Current?.TransactionInformation.LocalIdentifier;
        TransactionClone = Transaction.Current?.Clone();
        return ConnectionOpened.Task;
      }

      protected override Task SaveChangesCoreAsync(SaveWorkState saveWorkState, CancellationToken cancellationToken) {
        CoreThreadId = Environment.CurrentManagedThreadId;
        CoreTransactionId = Transaction.Current?.TransactionInformation.LocalIdentifier;
        saveWorkState.KeyMappings = new List<KeyMapping>();
        return Task.CompletedTask;
      }

      protected override Task CloseDbConnectionAsync() => Task.CompletedTask;
      public override IDbConnection GetDbConnection() => throw new NotSupportedException();
      protected override void OpenDbConnection() => throw new NotSupportedException();
      protected override void CloseDbConnection() => throw new NotSupportedException();
      protected override void SaveChangesCore(SaveWorkState saveWorkState) => throw new NotSupportedException();
      protected override string BuildJsonMetadata() => throw new NotSupportedException();
      protected override Task<IDbTransaction> BeginTransactionAsync(System.Data.IsolationLevel isolationLevel, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
  }
}
