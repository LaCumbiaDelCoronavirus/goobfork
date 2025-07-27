using System.Linq;
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
    public Solution Solution = new();

    public float CachedFillFraction;
    public float CachedAvailableVolume;

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

        // if the node is simply being removed into a separate group, we do nothing, as gas redistribution will be
        // handled by AfterRemake(). But if it is being deleted, we actually want to remove the gas stored in this node.
        // EDIT: Fuck that. We just do THIS.
        if (node is not PlumbingNode plumbing)
            return;

        Solution.SplitSolution(plumbing.Capacity);
        //Air.Multiply(1f - pipe.Volume / Air.Volume);
        Solution.MaxVolume -= plumbing.Capacity;
    }

    public override void AfterRemake(IEnumerable<IGrouping<INodeGroup?, Node>> newGroups)
    {
        _plumbingSystem?.RemovePlumbingNet(this);
        var splitVolume = Solution.MaxVolume / newGroups.Count();

        foreach (var newGroup in newGroups)
        {
            if (newGroup.Key is not PlumbingNet newNet)
                continue;

            newNet.Solution.AddSolution(Solution.SplitSolution(splitVolume), _prototypeManager);
        }

        Solution.MaxVolume = splitVolume;
    }
}
