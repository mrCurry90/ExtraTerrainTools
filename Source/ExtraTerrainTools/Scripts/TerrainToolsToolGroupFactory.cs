using System.Collections.Generic;
using Timberborn.BlockObjectTools;
using Timberborn.BlockSystem;
using Timberborn.BottomBarSystem;
using Timberborn.ToolSystem;

public class TerrainToolsToolGroupFactory
{
	private readonly BlockObjectToolButtonFactory _blockObjectToolButtonFactory;

	private readonly ToolGroupButtonFactory _toolGroupButtonFactory;

	public TerrainToolsToolGroupFactory(BlockObjectToolButtonFactory blockObjectToolButtonFactory, ToolGroupButtonFactory toolGroupButtonFactory)
	{
		_blockObjectToolButtonFactory = blockObjectToolButtonFactory;
		_toolGroupButtonFactory = toolGroupButtonFactory;
	}

	public BottomBarElement Create(ToolGroupSpec toolGroupSpecification, IEnumerable<PlaceableBlockObjectSpec> blockObjects)
	{
		BlockObjectToolGroup toolGroup = new BlockObjectToolGroup(toolGroupSpecification);
		ToolGroupButton toolGroupButton = _toolGroupButtonFactory.CreateGreen(toolGroup);
		foreach (PlaceableBlockObjectSpec blockObject in blockObjects)
		{
			if (blockObject.UsableWithCurrentFeatureToggles)
			{
				ToolButton button = _blockObjectToolButtonFactory.Create(blockObject, toolGroup, toolGroupButton.ToolButtonsElement);
				toolGroupButton.AddTool(button);
			}
		}
		return BottomBarElement.CreateMultiLevel(toolGroupButton.Root, toolGroupButton.ToolButtonsElement);
	}
}
