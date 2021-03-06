﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace Models.NorthwindIB.EDMX_2012 {

  [AttributeUsage(AttributeTargets.Class)] // NEW
  public class CustomerValidator : ValidationAttribute {
    public override Boolean IsValid(Object value) {
      var cust = value as Customer;
      if (cust != null && cust.CompanyName.ToLower() == "error") {
        ErrorMessage = "This customer is not valid!";
        return false;
      }
      return true;
    }
  }


  [AttributeUsage(AttributeTargets.Property)]
  public class CustomValidator : ValidationAttribute {
    public override Boolean IsValid(Object value) {
      try {
        var val = (string)value;
        if (!string.IsNullOrEmpty(val) && val.StartsWith("Error")) {
          ErrorMessage = "{0} equal the word 'Error'";
          return false;
        }
        return true;
      } catch (Exception e) {
        var x = e;
        return false;
      }
    }
  }

  [MetadataType(typeof(CustomerMetaData))]
  [CustomerValidator]
  public partial class Customer {
    
    public string ExtraString {
      get { return _extraString; }
      set { _extraString = value; }
    }

    
    public double ExtraDouble {
      get { return _extraDouble; }
      set { _extraDouble = value; }
    }

    private string _extraString = "fromServer";
    private double _extraDouble = 3.14159;

  }


  public class CustomerMetaData {


    [CustomValidator]
    public string ContactName {
      get;
      set;
    }



  }
}
