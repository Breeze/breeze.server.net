using Breeze.Persistence.EFCore;

namespace TestBreeze {
  /// <summary>
  /// Tests for Take clause handling in BreezeQueryFilter and BreezeAsyncQueryFilter, with MaxTake parameter.
  /// </summary>
  [TestClass]
  public sealed class TestMetadata {

    [TestMethod]
    public void TestMaxLength() {
      var dbx = Util.NorthwindIB();
      var meta = MetadataBuilder.BuildFrom(dbx);
      Assert.IsNotNull(meta);

      var employeeType = meta.StructuralTypes.FirstOrDefault(t => t.ShortName == "Employee");
      Assert.IsNotNull(employeeType);

      var employeeCountryProp = employeeType.DataProperties.FirstOrDefault(p => p.NameOnServer == "Country");
      Assert.IsNotNull(employeeCountryProp);
      Assert.AreEqual(15, employeeCountryProp.MaxLength);
      Assert.AreEqual(1, employeeCountryProp.Validators.Count);
      Assert.AreEqual("maxLength", employeeCountryProp.Validators[0].Name, true);

      var employeeNotesProp = employeeType.DataProperties.FirstOrDefault(p => p.NameOnServer == "Notes");
      Assert.IsNotNull(employeeNotesProp);
      Assert.IsNull(employeeNotesProp.MaxLength);
      Assert.AreEqual(0, employeeNotesProp.Validators.Count);

      var commentType = meta.StructuralTypes.FirstOrDefault(t => t.ShortName == "Comment");
      Assert.IsNotNull(commentType);

      var commentTextProp = commentType.DataProperties.FirstOrDefault(p => p.NameOnServer == "Comment1");
      Assert.IsNotNull(commentTextProp);
      Assert.AreEqual(-1, commentTextProp.MaxLength);
      Assert.AreEqual(0, commentTextProp.Validators.Count);

    }

  }
}
