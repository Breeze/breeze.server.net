using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inheritance.Models
{
    public class InheritanceDataMaker
    {
        private static int _idSeed = 1;
        private static DateTime _baseCreatedAt = new DateTime(2012, 8, 22, 9, 0, 0);
        private static DateTime _depositedAt = new DateTime(2012, 9, 1, 1, 0, 0);

        public static IList<AccountType> MakeAccountTypes()
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
            return accountTypes;
        }


        public static TBilling[] MakeData<TBilling, TBankAccount, TCreditCard>(string inheritanceModel)
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

        public static TDeposit[] MakeDeposits<TBilling, TDeposit>(TBilling billingDetail)
          where TBilling : IBillingDetail
          where TDeposit : class, IDeposit, new()
        {
            var account = billingDetail as IBankAccount;
            if (null == account) return Array.Empty<TDeposit>();

            var accountId = account.Id;
            var amount = 0;
            var deposits = new[]
                      {
                    new TDeposit {BankAccountId = accountId, Amount = (amount += 100), Deposited = _depositedAt},
                    new TDeposit {BankAccountId = accountId, Amount = (amount += 100), Deposited = _depositedAt},
                    new TDeposit {BankAccountId = accountId, Amount = (amount += 100), Deposited = _depositedAt},
               };
            return deposits;
        }

    }
}
