using SMP_SCADA.Core.Drive;

namespace SMP_SCADA.Core.Tests;

public class DriveObjectsTests
{
    [Fact]
    public void Keys_and_addresses_are_unique()
    {
        var all = DriveObjects.All;
        Assert.Equal(all.Count, all.Select(o => o.Key).Distinct().Count());
        Assert.Equal(all.Count, all.Select(o => (o.Index, o.SubIndex)).Distinct().Count());
        Assert.Equal(all.Count, DriveObjects.ByKey.Count);
    }

    [Fact]
    public void Every_named_object_is_in_All()
    {
        // Finds every DriveObject field by reflection
        var named = typeof(DriveObjects).GetFields()
            .Where(f => f.FieldType == typeof(DriveObject))
            .Select(f => (DriveObject)f.GetValue(null)!)
            .ToList();

        Assert.Equal(named.Count, DriveObjects.All.Count);
        Assert.All(named, o => Assert.Contains(o, DriveObjects.All));
    }
}