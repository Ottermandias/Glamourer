using Glamourer.Automation;
using Newtonsoft.Json.Linq;
using OtterGui.Filesystem;

namespace Glamourer.Designs.Links;

public sealed class LinkContainer : List<DesignLink>
{
    public List<DesignLink> Before
        => this;

    public readonly List<DesignLink> After = [];

    public new int Count
        => base.Count + After.Count;

    public LinkContainer Clone()
    {
        var ret = new LinkContainer();
        ret.EnsureCapacity(base.Count);
        ret.After.EnsureCapacity(After.Count);
        ret.AddRange(this);
        ret.After.AddRange(After);
        return ret;
    }

    public bool Reorder(int fromIndex, LinkOrder fromOrder, int toIndex, LinkOrder toOrder)
    {
        var fromList = fromOrder switch
        {
            LinkOrder.Before => Before,
            LinkOrder.After  => After,
            _                => throw new ArgumentException("Invalid link order."),
        };

        var toList = toOrder switch
        {
            LinkOrder.Before => Before,
            LinkOrder.After  => After,
            _                => throw new ArgumentException("Invalid link order."),
        };

        if (fromList == toList)
            return fromList.Move(fromIndex, toIndex);

        if (fromIndex < 0 || fromIndex >= fromList.Count)
            return false;

        toIndex = Math.Clamp(toIndex, 0, toList.Count);
        toList.Insert(toIndex, fromList[fromIndex]);
        fromList.RemoveAt(fromIndex);
        return true;
    }

    public bool Remove(int idx, LinkOrder order)
    {
        var list = order switch
        {
            LinkOrder.Before => Before,
            LinkOrder.After  => After,
            _                => throw new ArgumentException("Invalid link order."),
        };
        if (idx < 0 || idx >= list.Count)
            return false;

        list.RemoveAt(idx);
        return true;
    }

    public bool ChangeApplicationRules(int idx, LinkOrder order, ApplicationType type, out ApplicationType old)
    {
        var list = order switch
        {
            LinkOrder.Before => Before,
            LinkOrder.After  => After,
            _                => throw new ArgumentException("Invalid link order."),
        };
        old = list[idx].Type;
        if (idx < 0 || idx >= list.Count || old == type)
            return false;

        list[idx] = list[idx] with { Type = type };
        return true;
    }

    public static bool CanAddLink(Design parent, Design child, LinkOrder order, out string error)
    {
        if (parent == child)
        {
            error = $"Can not link {parent.Incognito} with itself.";
            return false;
        }

        if (parent.Links.Contains(child))
        {
            error = $"Design {parent.Incognito} already contains a direct link to {child.Incognito}.";
            return false;
        }

        if (GetAllLinks(parent).Any(l => l.Link.Link == child && l.Order != order))
        {
            error =
                $"Adding {child.Incognito} to {parent.Incognito}s links would create a circle, the parent already links to the child in the opposite direction.";
            return false;
        }

        if (GetAllLinks(child).Any(l => l.Link.Link == parent && l.Order == order))
        {
            error =
                $"Adding {child.Incognito} to {parent.Incognito}s links would create a circle, the child already links to the parent in the opposite direction.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static bool AddLink(Design parent, Design child, ApplicationType type, LinkOrder order, out string error)
    {
        if (!CanAddLink(parent, child, order, out error))
            return false;

        var list = order switch
        {
            LinkOrder.Before => parent.Links.Before,
            LinkOrder.After  => parent.Links.After,
            _                => null,
        };

        if (list == null)
        {
            error = $"Order {order} is invalid.";
            return false;
        }

        type &= ApplicationType.All;
        list.Add(new DesignLink(child, type));
        error = string.Empty;
        return true;
    }

    public bool Contains(Design child)
        => Before.Any(l => l.Link == child) || After.Any(l => l.Link == child);

    public bool Remove(Design child)
        => Before.RemoveAll(l => l.Link == child) + After.RemoveAll(l => l.Link == child) > 0;

    public static IEnumerable<(DesignLink Link, LinkOrder Order)> GetAllLinks(Design design)
    {
        var set = new HashSet<Design>(design.Links.Count * 4);
        return GetAllLinks(new DesignLink(design, ApplicationType.All), LinkOrder.Self, set);
    }

    private static IEnumerable<(DesignLink Link, LinkOrder Order)> GetAllLinks(DesignLink design, LinkOrder currentOrder, ISet<Design> visited)
    {
        if (design.Link.Links.Count == 0)
        {
            if (visited.Add(design.Link))
                yield return (design, currentOrder);

            yield break;
        }

        foreach (var link in design.Link.Links.Before
                     .Where(l => !visited.Contains(l.Link))
                     .SelectMany(l => GetAllLinks(l, currentOrder == LinkOrder.After ? LinkOrder.After : LinkOrder.Before, visited)))
            yield return link;

        if (visited.Add(design.Link))
            yield return (design, currentOrder);

        foreach (var link in design.Link.Links.After.Where(l => !visited.Contains(l.Link))
                     .SelectMany(l => GetAllLinks(l, currentOrder == LinkOrder.Before ? LinkOrder.Before : LinkOrder.After, visited)))
            yield return link;
    }

    public JObject Serialize()
    {
        var before = new JArray();
        foreach (var link in Before)
        {
            before.Add(new JObject
            {
                ["Design"] = link.Link.Identifier,
                ["Type"]   = (uint)link.Type,
            });
        }

        var after = new JArray();
        foreach (var link in After)
        {
            after.Add(new JObject
            {
                ["Design"] = link.Link.Identifier,
                ["Type"]   = (uint)link.Type,
            });
        }

        return new JObject
        {
            [nameof(Before)] = before,
            [nameof(After)]  = after,
        };
    }
}
