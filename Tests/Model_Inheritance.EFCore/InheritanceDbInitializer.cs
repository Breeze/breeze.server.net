using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Inheritance.Models
{
    // DEMONSTRATION/DEVELOPMENT ONLY
    public class InheritanceDbInitializer
    {
        // private static IList<IDeposit> _deposits;
        private static IList<AccountType> _bankAccountTypes;

        private static int _idSeed = 1;
        private static DateTime _baseCreatedAt = new DateTime(2012, 8, 22, 9, 0, 0);
        private static DateTime _depositedAt = new DateTime(2012, 9, 1, 1, 0, 0);

        public static void Seed(InheritanceContext context)
        {
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            _bankAccountTypes = AddAccountTypes(context);
            ResetDatabase(context);
        }

        private static IList<AccountType> AddAccountTypes(InheritanceContext context)
        {
            var accountTypes = new List<AccountType>
                {
                    new AccountType {Id = 1, Name = "Checking"},
                    new AccountType {Id = 2, Name = "Saving"},
                    new AccountType {Id = 3, Name = "Money Market"},

                    new AccountType {Id = 4, Name = "Amex"},
                    new AccountType {Id = 5, Name = "MC"},
                    new AccountType {Id = 6, Name = "Visa"}
                };
            accountTypes.ForEach(_ => context.AccountTypes.Add(_));
            return accountTypes;
        }

        public static void ResetDatabase(InheritanceContext context)
        {

            IBillingDetail[] billingDetails;

            billingDetails = MakeData<BillingDetailTPH, BankAccountTPH, CreditCardTPH>("TPH");
            Array.ForEach((BillingDetailTPH[])billingDetails, _ => {
                context.BillingDetailTPHs.Add(_);
                AddDeposits(_, context.DepositTPHs);
            });

#if NET5_0_OR_GREATER
            // TPT supported in EFCore 5 but not 3
            billingDetails = MakeData<BillingDetailTPT, BankAccountTPT, CreditCardTPT>("TPT");
            Array.ForEach((BillingDetailTPT[])billingDetails, _ => {
                context.BillingDetailTPTs.Add(_);
                AddDeposits(_, context.DepositTPTs);
            });
#endif
            _idSeed = 1; // reset for TPC ... because we can
            billingDetails = MakeData<BillingDetailTPC, BankAccountTPC, CreditCardTPC>("TPC");
            Array.ForEach((BillingDetailTPC[])billingDetails, _ => {
                // TPC not supported in EF Core 5
                //context.BillingDetailTPC.Add(_);
                context.Add(_);
                AddDeposits(_, context.DepositTPCs);
            });

            context.SaveChanges(); // Save all inserts
        }

        private static TBilling[] MakeData<TBilling, TBankAccount, TCreditCard>(string inheritanceModel)
          where TBilling : IBillingDetail
          where TBankAccount : TBilling, IBankAccount, new()
          where TCreditCard : TBilling, ICreditCard, new()
        {

            var billingDetails = new[] {
                // Owner, Number, AccountTypeId, ExpiryMonth, ExpiryYear
                (TBilling) CreateCreditCard<TCreditCard>("Abby Road"    , "999-999-999", 4, "04", "2014"),
                (TBilling) CreateCreditCard<TCreditCard>("Bobby Tables" , "987-654-321", 6, "03", "2014"),

                // Owner, Number, AccountTypeId, BankName, Swift
                (TBilling) CreateBankAccount<TBankAccount>("Cathy Corner", "123-456", 1, "Bank of Fun", "BOFFDEFX"),
                (TBilling) CreateBankAccount<TBankAccount>("Early Riser" , "11-11-1111", 2, "Snake Eye Bank", "SNEBSSSS"),
                (TBilling) CreateBankAccount<TBankAccount>("Dot Com"     , "777-777", 3, "Bank of Sevens", "BOFSWXYZ"),

                (TBilling) CreateCreditCard<TCreditCard>("Ginna Lovette", "111-222-333", 5, "02", "2014"),
                (TBilling) CreateCreditCard<TCreditCard>("Faith Long"   , "123-456-789", 4, "04", "2015")
           };
            Array.ForEach(billingDetails, _ => _.InheritanceModel = inheritanceModel);

            return billingDetails;
        }

        private static IBillingDetail CreateBankAccount<T>(
            string owner, string number, int accountTypeId, string bankName, string swift)
            where T : IBankAccount, new()
        {
            _baseCreatedAt = _baseCreatedAt.AddMinutes(1);
            return new T
            {
                Id = _idSeed++,
                CreatedAt = _baseCreatedAt,
                Owner = owner,
                Number = number,
                BankName = bankName,
                Swift = swift,
                AccountTypeId = accountTypeId
            };
        }

        private static IBillingDetail CreateCreditCard<T>(
            string owner, string number, int accountTypeId, string expiryMonth, string expiryYear)
            where T : ICreditCard, new()
        {
            _baseCreatedAt = _baseCreatedAt.AddMinutes(1);
            return new T
            {
                Id = _idSeed++,
                CreatedAt = _baseCreatedAt,
                Owner = owner,
                Number = number,
                AccountTypeId = accountTypeId,
                ExpiryMonth = expiryMonth,
                ExpiryYear = expiryYear
            };
        }

        private static void AddDeposits<TBilling, TDeposit>(TBilling billingDetail, DbSet<TDeposit> dbset)
          where TBilling : IBillingDetail
          where TDeposit : class, IDeposit, new()
        {
            var account = billingDetail as IBankAccount;
            if (null == account) return;

            var accountId = account.Id;
            var amount = 0;
            var deposits = new[]
                      {
                    new TDeposit {BankAccountId = accountId, Amount = (amount += 100), Deposited = _depositedAt},
                    new TDeposit {BankAccountId = accountId, Amount = (amount += 100), Deposited = _depositedAt},
                    new TDeposit {BankAccountId = accountId, Amount = (amount += 100), Deposited = _depositedAt},
               };
            Array.ForEach(deposits, _ => dbset.Add(_));
        }

        public static void PurgeDatabase(InheritanceContext context)
        {
            context.RemoveRange(context.DepositTPHs);
            context.RemoveRange(context.DepositTPTs);
            context.RemoveRange(context.DepositTPCs);
            context.RemoveRange(context.BillingDetailTPHs);
#if NET5_0_OR_GREATER
            context.RemoveRange(context.BillingDetailTPTs);
            context.RemoveRange(context.BankAccountTPTs);
            context.RemoveRange(context.CreditCardTPTs);
#endif
            //context.RemoveRange(context.BillingDetailTPCs);
            context.RemoveRange(context.BankAccountTPCs);
            context.RemoveRange(context.CreditCardTPCs);

            context.SaveChanges();
        }
    }
}
