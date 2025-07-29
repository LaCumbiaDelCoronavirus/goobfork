using System.Linq;
using Content.Goobstation.Maths.FixedPoint;
using Content.Goobstation.Server.Plumbing.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Shared.Chemistry.Components;
using Content.Shared.NodeContainer;
using Content.Shared.NodeContainer.NodeGroups;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Server.Plumbing;

[NodeGroup(NodeGroupID.Plumbing)]
public sealed class PlumbingNet : BaseNodeGroup, INodeGroup
{
    private IPrototypeManager? _prototypeManager;
    private PlumbingSystem? _plumbingSystem;

    [ViewVariables(VVAccess.ReadWrite)]
    public Solution Solution = new() { CanReact = false };

    public float CachedFillFraction;
    public float CachedAvailableVolume;

    /// <summary>
    ///     A very large solution that will be removed from this net's solution and emptied, the next time this is processed.
    /// </summary>
    public Solution QueuedOutput = new(int.MaxValue) { CanReact = false };
    /// <summary>
    ///     A very large solution that will be added to this net's solution and emptied, the next time this is processed.
    /// </summary>
    public Solution QueuedInput = new(int.MaxValue) { CanReact = false };

    [ViewVariables(VVAccess.ReadOnly)]
    public float AvailableVolume => Solution.AvailableVolume.Float();

    public override void Initialize(Node sourceNode, IEntityManager entMan)
    {
        base.Initialize(sourceNode, entMan);

        IoCManager.Resolve(ref _prototypeManager);
        _plumbingSystem = entMan.System<PlumbingSystem>();

        _plumbingSystem.AddPlumbingNet(this);
    }
    public override void LoadNodes(List<Node> groupNodes)
    {
        base.LoadNodes(groupNodes);

        foreach (var node in groupNodes)
        {
            var plumbingNode = (PlumbingNode) node;
            Solution.MaxVolume += plumbingNode.Capacity;
        }
    }

    public override void RemoveNode(Node node)
    {
        base.RemoveNode(node);

        // This should only handle nodes that aren't handled by AfterRemake.
        if (!node.Deleting || node is not PlumbingNode plumbing)
            return;

        Solution.SplitSolution(plumbing.Capacity);
        Solution.MaxVolume -= plumbing.Capacity;
    }

    public override void AfterRemake(IEnumerable<IGrouping<INodeGroup?, Node>> newGroups)
    {
        _plumbingSystem?.RemovePlumbingNet(this);

        var cached = Solution.Clone();
        foreach (var newGroup in newGroups)
        {
            if (newGroup.Key is not PlumbingNet net)
                continue;

            // The fraction of fluid that, from our net, this new net gets.
            var fraction = net.Solution.MaxVolume / Solution.MaxVolume;
            net.Solution.AddSolution(cached.SplitSolution(net.Solution.MaxVolume), _prototypeManager);
        }
    }

    public override string GetDebugData()
        => @$"Volume: {(float) Solution.Volume:G3} / {(float) Solution.MaxVolume:G3}";
}
