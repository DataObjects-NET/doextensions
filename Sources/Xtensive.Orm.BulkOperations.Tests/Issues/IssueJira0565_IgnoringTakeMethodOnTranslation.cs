// Copyright (C) 2015 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexey Kulakov
// Created:    2015.02.06

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using TestCommon.Model;
using Xtensive.Orm.Providers;
using Xtensive.Orm.Services;

namespace Xtensive.Orm.BulkOperations.Tests.Issues
{
  public class IssueJira0565_IgnoringTakeMethodOnTranslation : AutoBuildTest
  {
    [Test]
    public void UpdateOperationWithoutLimitation01()
    {
      using (var session = Domain.OpenSession())
      using (session.Activate())
      using (var transaction = session.OpenTransaction()) {
        var updated = session.Query.All<Bar>().Set(el => el.Description, "UpdatedAgain").Update();
        Assert.AreEqual(250, updated);
        var updatedList = session.Query.All<Bar>().Where(el => el.Description=="UpdatedAgain").ToList();
        Assert.AreEqual(250, updatedList.Count);
      }
    }

    [Test]
    public void UpdateOperationWithoutLimitation02()
    {
      using (var session = Domain.OpenSession())
      using (session.Activate())
      using (var transaction = session.OpenTransaction()) {
        var updated = session.Query.All<Bar>().Where(el=>el.Id < 51).Union(session.Query.All<Bar>().Where(el=>el.Id > 200)).Set(el => el.Description, "UpdatedAgain").Update();
        Assert.AreEqual(100, updated);
        var updatedList = session.Query.All<Bar>().Where(el => el.Description=="UpdatedAgain").ToList();
        Assert.AreEqual(100, updatedList.Count);
      }
    }

    [Test]
    public void DeleteOperationWithoutLimitation01()
    {
      using (var session = Domain.OpenSession())
      using (session.Activate())
      using (var transaction = session.OpenTransaction()) {
        var updated = session.Query.All<Bar>().Delete();
        Assert.AreEqual(250, updated);
        var updatedList = session.Query.All<Bar>().ToList();
        Assert.AreEqual(0, updatedList.Count);
      }
    }

    [Test]
    public void DeleteOperationWithoutLimitation02()
    {
      using (var session = Domain.OpenSession())
      using (session.Activate())
      using (var transaction = session.OpenTransaction()) {
        var updated = session.Query.All<Bar>().Where(el => el.Id < 51).Union(session.Query.All<Bar>().Where(el => el.Id > 200)).Delete();
        Assert.AreEqual(100, updated);
        var updatedList = session.Query.All<Bar>().ToList();
        Assert.AreEqual(150, updatedList.Count);
      }
    }

    [Test]
    public void UpdateOperationWithLimitation01()
    {
      SupportsUpdateLimitation();
      using (var session = Domain.OpenSession())
      using (session.Activate())
      using (var transaction = session.OpenTransaction()) {
        var updated = session.Query.All<Bar>().Take(200).Set(el => el.Description, "UpdatedAgain").Update();
        Assert.AreEqual(200, updated);
        var updatedList = session.Query.All<Bar>().Where(el => el.Description=="UpdatedAgain").ToList();
        Assert.AreEqual(200, updatedList.Count);
      }
    }

    [Test]
    public void UpdateOperationWithLimitation02()
    {
      SupportsUpdateLimitation();
      using (var session = Domain.OpenSession())
      using (session.Activate())
      using (var transaction = session.OpenTransaction()) {
        var aaa = session.Query.All<Bar>().Where(el => el.Id < 100).Take(50).Union(session.Query.All<Bar>().Where(el => el.Id > 100).Take(50)).ToList();
        var updated = session.Query.All<Bar>().Where(el=>el.Id < 100).Take(50).Union(session.Query.All<Bar>().Where(el => el.Id > 100).Take(50)).Set(el => el.Description, "UpdatedAgain").Update();
        Assert.AreEqual(200, updated);
        var updatedList = session.Query.All<Bar>().Where(el => el.Description=="UpdatedAgain").ToList();
        Assert.AreEqual(200, updatedList.Count);
      }
    }

    [Test]
    public void UpdateOperationWithLimitation03()
    {
      SupportsUpdateLimitation();
      using (var session = Domain.OpenSession())
      using (session.Activate())
      using (var transaction = session.OpenTransaction()) {
        var updated = session.Query.All<Bar>().Where(el => el.Id < 100).Union(session.Query.All<Bar>().Where(el => el.Id > 100)).Take(100).Set(el => el.Description, "UpdatedAgain").Update();
        Assert.AreEqual(100, updated);
        var updatedList = session.Query.All<Bar>().Where(el => el.Description == "UpdatedAgain").ToList();
        Assert.AreEqual(100, updatedList.Count);
      }
    }

    [Test]
    [ExpectedException(typeof(NotSupportedException))]
    public void UpdateOperationWithLimitation04()
    {
      DoesNotSupportsUpdateLimitation();
      using (var session = Domain.OpenSession())
      using (session.Activate())
      using (var transaction = session.OpenTransaction()){
        var updated = session.Query.All<Bar>().Take(200).Set(el => el.Description, "UpdatedAgain").Update();
        Assert.AreEqual(200, updated);
        var updatedList = session.Query.All<Bar>().Where(el => el.Description == "UpdatedAgain").ToList();
        Assert.AreEqual(200, updatedList.Count);
      }
    }

    [Test]
    [ExpectedException(typeof(NotSupportedException))]
    public void UpdateOperationWithLimitation05()
    {
      DoesNotSupportsUpdateLimitation();
      using (var session = Domain.OpenSession())
      using (session.Activate())
      using (var transaction = session.OpenTransaction()) {
        var updated = session.Query.All<Bar>().Where(el => el.Id < 100).Take(50).Union(session.Query.All<Bar>().Where(el => el.Id > 100).Take(50)).Set(el => el.Description, "UpdatedAgain").Update();
        Assert.AreEqual(100, updated);
        var updatedList = session.Query.All<Bar>().Where(el => el.Description == "UpdatedAgain").ToList();
        Assert.AreEqual(100, updatedList.Count);
      }
    }

    [Test]
    [ExpectedException(typeof (NotSupportedException))]
    public void UpdateOperationWithLimitation06()
    {
      DoesNotSupportsUpdateLimitation();
      using (var session = Domain.OpenSession())
      using (session.Activate())
      using (var transaction = session.OpenTransaction()) {
        var updated = session.Query.All<Bar>().Where(el => el.Id < 100).Union(session.Query.All<Bar>().Where(el => el.Id > 100)).Take(100).Set(el => el.Description, "UpdatedAgain").Update();
        Assert.AreEqual(100, updated);
        var updatedList = session.Query.All<Bar>().Where(el => el.Description == "UpdatedAgain").ToList();
        Assert.AreEqual(100, updatedList.Count);
      }
    }

    [Test]
    public void DeleteOperationWithLimitation01()
    {
      SupportsDeleteLimitation();
      using (var session = Domain.OpenSession())
      using (session.Activate())
      using (var transaction = session.OpenTransaction()) {
        var updated = session.Query.All<Bar>().Take(100).Delete();
        Assert.AreEqual(100, updated);
        var updatedList = session.Query.All<Bar>().ToList();
        Assert.AreEqual(150, updatedList.Count);
      }
    }

    [Test]
    public void DeleteOperationWithLimitation02()
    {
      SupportsDeleteLimitation();
      using (var session = Domain.OpenSession())
      using (session.Activate())
      using (var transaction = session.OpenTransaction()) {
        var updated = session.Query.All<Bar>().Where(el => el.Id < 100).Take(50).Union(session.Query.All<Bar>().Where(el => el.Id > 100).Take(50)).Delete();
        Assert.AreEqual(200, updated);
        var updatedList = session.Query.All<Bar>().ToList();
        Assert.AreEqual(50, updatedList.Count);
      }
    }

    [Test]
    public void DeleteOperationWithLimitation03()
    {
      SupportsDeleteLimitation();
      using (var session = Domain.OpenSession())
      using (session.Activate())
      using (var transaction = session.OpenTransaction()) {
        var updated = session.Query.All<Bar>().Where(el => el.Id < 100).Union(session.Query.All<Bar>().Where(el => el.Id > 100)).Take(100).Delete();
        Assert.AreEqual(100, updated);
        var updatedList = session.Query.All<Bar>().ToList();
        Assert.AreEqual(150, updatedList.Count);
      }
    }

    [Test]
    [ExpectedException(typeof(NotSupportedException))]
    public void DeleteOperationWithLimitation04()
    {
      DoesNotSupportsDeleteLimitation();
      using (var session = Domain.OpenSession())
      using (session.Activate())
      using (var transaction = session.OpenTransaction()) {
        var updated = session.Query.All<Bar>().Take(100).Delete();
        Assert.AreEqual(100, updated);
        var updatedList = session.Query.All<Bar>().ToList();
        Assert.AreEqual(150, updatedList.Count);
      }
    }

    [Test]
    [ExpectedException(typeof(NotSupportedException))]
    public void DeleteOperationWithLimitation05()
    {
      DoesNotSupportsDeleteLimitation();
      using (var session = Domain.OpenSession())
      using (session.Activate())
      using (var transaction = session.OpenTransaction()) {
        var updated = session.Query.All<Bar>().Where(el => el.Id < 100).Take(50).Union(session.Query.All<Bar>().Where(el => el.Id > 100).Take(50)).Delete();
        Assert.AreEqual(100, updated);
        var updatedList = session.Query.All<Bar>().ToList();
        Assert.AreEqual(150, updatedList.Count);
      }
    }

    [Test]
    [ExpectedException(typeof(NotSupportedException))]
    public void DeleteOperationWithLimitation06()
    {
      DoesNotSupportsDeleteLimitation();
      using (var session = Domain.OpenSession())
      using (session.Activate())
      using (var transaction = session.OpenTransaction()) {
        var updated = session.Query.All<Bar>().Where(el => el.Id < 100).Union(session.Query.All<Bar>().Where(el => el.Id > 100)).Take(100).Delete();
        Assert.AreEqual(100, updated);
        var updatedList = session.Query.All<Bar>().ToList();
        Assert.AreEqual(150, updatedList.Count);
      }
    }

    [Test]
    public void UpdateOperationTableAsSource()
    {
      using (var session = Domain.OpenSession())
      using (session.Activate())
      using (var transaction = session.OpenTransaction()) {
        var list = session.Query.All<Bar>().Take(200).ToList();
        Assert.AreEqual(200, list.Count);
        var updated = session.Query.All<Bar>().Take(200).Set(el => el.Description, "Updated").Update();
        Assert.AreEqual(200, updated);
        var updatedList = session.Query.All<Bar>().Where(el => el.Description=="Updated").ToList();
        Assert.AreEqual(200, updatedList.Count);
        updated = session.Query.All<Bar>().Set(el => el.Description, "UpdatedAgain").Update();
        Assert.AreEqual(250, updated);
        updatedList = session.Query.All<Bar>().Where(el => el.Description=="UpdatedAgain").ToList();
        Assert.AreEqual(250, updatedList.Count);
      }
    }

    [Test]
    public void UpdateOperationUnionAsSource()
    {
      using (var session = Domain.OpenSession())
      using (session.Activate())
      using (var transaction = session.OpenTransaction()) {
        var list = session.Query.All<Bar>().Where(el => el.Id < 100).Take(50).Union(session.Query.All<Bar>().Where(el => el.Id > 100).Take(50)).ToList();
        Assert.AreEqual(100, list.Count);
        var updated = session.Query.All<Bar>().Where(el => el.Id < 100).Take(50).Union(session.Query.All<Bar>().Where(el => el.Id > 100).Take(50)).Set(el => el.Description, "Updated").Update();
        Assert.AreEqual(100, updated);
        var updatedList = session.Query.All<Bar>().Where(el => el.Description == "Updated").ToList();
        Assert.AreEqual(100, updatedList.Count);
        updated = session.Query.All<Bar>().Set(el => el.Description, "UpdatedAgain").Update();
        Assert.AreEqual(250, updated);
        updatedList = session.Query.All<Bar>().Where(el => el.Description == "UpdatedAgain").ToList();
        Assert.AreEqual(250, updatedList.Count);
      }
    }

    [Test]
    public void DeleteOperationTableAsSource()
    {
      
    }

    protected override void PopulateData()
    {
      base.PopulateData();
      using (var session = Domain.OpenSession())
      using (session.Activate())
      using (var transaction = session.OpenTransaction()) {
        for (int i = 0; i< 250; i++) {
          new Bar(session);
        }
        transaction.Complete();
      }
    }

    protected override Configuration.DomainConfiguration BuildConfiguration()
    {
      var configuration = base.BuildConfiguration();
      configuration.UpgradeMode = DomainUpgradeMode.Recreate;
      return configuration;
    }

    private void SupportsUpdateLimitation()
    {
      if (!Domain.StorageProviderInfo.Supports(ProviderFeatures.UpdateLimit) &&
        !Domain.StorageProviderInfo.Supports(ProviderFeatures.UpdateFrom))
        IgnoreMe("This provider does not support limitation of affecred rows on update.", null);
    }

    private void DoesNotSupportsUpdateLimitation()
    {
      if (Domain.StorageProviderInfo.Supports(ProviderFeatures.UpdateLimit) ||
        Domain.StorageProviderInfo.Supports(ProviderFeatures.UpdateFrom))
        IgnoreMe("This provider supports update limitation", null);
    }

    private void SupportsDeleteLimitation()
    {
      if (!Domain.StorageProviderInfo.Supports(ProviderFeatures.DeleteLimit) &&
        !Domain.StorageProviderInfo.Supports(ProviderFeatures.DeleteFrom))
        IgnoreMe("This provider does not support limitation of affecred rows on delet.", null);
    }

    private void DoesNotSupportsDeleteLimitation()
    {
      if (Domain.StorageProviderInfo.Supports(ProviderFeatures.DeleteLimit) ||
        Domain.StorageProviderInfo.Supports(ProviderFeatures.DeleteFrom))
        IgnoreMe("This provider support delete limitation", null);
    }

    private static void IgnoreMe(string format, object argument, string reason = null)
    {
      var message = string.Format(format, argument);
      if (!string.IsNullOrEmpty(reason))
        message = string.Format("{0}. Reason: {1}", message, reason);
      throw new IgnoreException(message);
    }
  }
}
