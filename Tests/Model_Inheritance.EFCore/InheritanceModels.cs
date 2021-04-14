﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

// Inheritance test models inspired by Morteza Manavi's posts on EF inheritance
//http://weblogs.asp.net/manavi/archive/2010/12/24/inheritance-mapping-strategies-with-entity-framework-code-first-ctp5-part-1-table-per-hierarchy-tph.aspx
namespace Inheritance.Models
{
    #region interfaces

    public interface IBillingDetail
    {
        int Id { get; set; }
        DateTime CreatedAt { get; set; }
        string Owner { get; set; }
        string Number { get; set; }

        // "InheritanceModel" makes it easier to test for the received type
        string InheritanceModel { get; set; } // "TPH", "TPT", "TPC"
    }

    public interface IBankAccount : IBillingDetail
    {
        string BankName { get; set; }
        string Swift { get; set; }
        int AccountTypeId { get; set; }
        AccountType AccountType { get; set; }
    }

    public interface ICreditCard : IBillingDetail
    {
        int AccountTypeId { get; set; }
        AccountType AccountType { get; set; } // FKA “CardType”
        string ExpiryMonth { get; set; }
        string ExpiryYear { get; set; }
    }

    public interface IDeposit
    {
        int Id { get; set; }
        int BankAccountId { get; set; }
        float Amount { get; set; }
        DateTime Deposited { get; set; }
    }

    public class AccountType : EntityBase
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    ///<summary>
    /// Base class for that might have business logic.
    /// Is invisible to EF and metadata because it has no mapped properties
    /// </summary>
    public class EntityBase
    {
        // Methods are invisible
        public void DoNothing() { }

        // Internals are invisible to EF and JSON.NET by default
        internal DateTime InternalDate { get; set; }

        // Marked [NotMapped] and therefore invisible to EF.
        // It won't be in metadata but it will be serialized to the client!
        [NotMapped]
        public int UnmappedInt { get; set; }

        // Hidden from both EF and the client
        //[JsonIgnore]
        [NotMapped]
        public string HiddenString { get; set; }
    }

    #endregion

    #region TPH

    public abstract partial class BillingDetailTPH : EntityBase, IBillingDetail
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Owner { get; set; }
        public string Number { get; set; }
        public int AccountTypeId { get; set; }
        public AccountType AccountType { get; set; }
        public string InheritanceModel { get; set; }
    }

    public class BankAccountTPH : BillingDetailTPH, IBankAccount
    {
        public string BankName { get; set; }
        public string Swift { get; set; }
        public ICollection<DepositTPH> Deposits { get; set; }
    }

    public class CreditCardTPH : BillingDetailTPH, ICreditCard
    {
        public string ExpiryMonth { get; set; }
        public string ExpiryYear { get; set; }
    }

    public partial class DepositTPH : EntityBase, IDeposit
    {
        public int Id { get; set; }
        public int BankAccountId { get; set; }
        public BankAccountTPH BankAccount { get; set; }
        public float Amount { get; set; }
        public DateTime Deposited { get; set; }
    }

    #endregion

    #region TPT

#if NETSTANDARD
    // TPT not supported in EF Core 3
    [NotMapped]
#endif
    public abstract partial class BillingDetailTPT : EntityBase, IBillingDetail
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Owner { get; set; }
        public string Number { get; set; }
        public int AccountTypeId { get; set; }
        public AccountType AccountType { get; set; }
        public string InheritanceModel { get; set; }
    }

    public class BankAccountTPT : BillingDetailTPT, IBankAccount
    {
        public string BankName { get; set; }
        public string Swift { get; set; }
        public ICollection<DepositTPT> Deposits { get; set; }
    }

    public class CreditCardTPT : BillingDetailTPT, ICreditCard
    {
        public string ExpiryMonth { get; set; }
        public string ExpiryYear { get; set; }
    }

    public partial class DepositTPT : EntityBase, IDeposit
    {
        public int Id { get; set; }
        public int BankAccountId { get; set; }
        public BankAccountTPT BankAccount { get; set; }
        public float Amount { get; set; }
        public DateTime Deposited { get; set; }
    }
    #endregion

    #region TPC

    // EF Core does not support TPC
    [NotMapped]
    public abstract partial class BillingDetailTPC : EntityBase, IBillingDetail
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Owner { get; set; }
        public string Number { get; set; }
        public string InheritanceModel { get; set; }
    }

    public class BankAccountTPC : BillingDetailTPC, IBankAccount
    {
        public string BankName { get; set; }
        public string Swift { get; set; }
        public int AccountTypeId { get; set; }
        public AccountType AccountType { get; set; }
        public ICollection<DepositTPC> Deposits { get; set; }
    }

    public class CreditCardTPC : BillingDetailTPC, ICreditCard
    {
        public int AccountTypeId { get; set; }
        public AccountType AccountType { get; set; }
        public string ExpiryMonth { get; set; }
        public string ExpiryYear { get; set; }
    }

    public partial class DepositTPC : EntityBase, IDeposit
    {
        public int Id { get; set; }
        public int BankAccountId { get; set; }
        public BankAccountTPC BankAccount { get; set; }
        public float Amount { get; set; }
        public DateTime Deposited { get; set; }
    }
    #endregion

}
