/***
 * Copyright (c) 2018 HNB-RaBear
 * https://github.com/hnb-rabear
 */

using System;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace RCore.SheetX.Editor
{
	/// <summary>
	/// Draws per-sheet collection output controls and persists their binding.
	/// </summary>
	internal static class SheetXCollectionSheetGUI
	{
		private static readonly string[] s_outputModes =
		{
			"JSON Only",
			"Generated Data Class",
			"Existing Data Class",
		};

		private static Type[] s_rowTypes;

		private sealed class RowTypeDropdown : AdvancedDropdown
		{
			private readonly Action<Type> m_onSelected;

			internal RowTypeDropdown(Action<Type> onSelected)
				: base(new AdvancedDropdownState())
			{
				m_onSelected = onSelected;
			}

			protected override AdvancedDropdownItem BuildRoot()
			{
				var root = new AdvancedDropdownItem("Data Class");
				if (s_rowTypes.Length == 0)
				{
					root.AddChild(new AdvancedDropdownItem("No [SheetXBindable] type found")
					{
						id = -1,
						enabled = false,
					});
					return root;
				}
				for (int index = 0; index < s_rowTypes.Length; index++)
					root.AddChild(new AdvancedDropdownItem(s_rowTypes[index].FullName) { id = index });
				return root;
			}

			protected override void ItemSelected(AdvancedDropdownItem item)
			{
				m_onSelected(item.id >= 0 && item.id < s_rowTypes.Length
					? s_rowTypes[item.id] : null);
			}
		}

		[InitializeOnLoadMethod]
		private static void ClearRowTypesAfterReload()
		{
			s_rowTypes = null;
		}

		internal static void AddColumns(
			EditorTableView<SheetPath> table,
			SheetXSettings settings,
			Func<string> sourceId)
		{
			table.AddColumn("Output Mode", 110, 140, (rect, item) =>
			{
				if (SheetXCollectionSettings.IsAutomaticConfiguration(settings, item?.name))
				{
					EditorGUI.LabelField(rect, "Automatic");
					return;
				}
				if (!TryBinding(settings, sourceId, item, out var binding))
					return;
				var mode = (SheetXSheetOutputMode)EditorGUI.Popup(
					rect, (int)binding.outputMode, s_outputModes);
				if (mode == binding.outputMode)
					return;
				binding.outputMode = mode;
				if (mode == SheetXSheetOutputMode.JsonOnly)
				{
					binding.collectionName = SheetXCollectionSettings.GlobalName;
					binding.rowTypeName = "";
				}
				settings.SaveToDisk();
			});

			table.AddColumn("Collection", 100, 140, (rect, item) =>
			{
				if (SheetXCollectionSettings.IsAutomaticConfiguration(settings, item?.name))
				{
					EditorGUI.LabelField(rect, "Global");
					return;
				}
				if (!TryBinding(settings, sourceId, item, out var binding)
					|| binding.outputMode == SheetXSheetOutputMode.JsonOnly)
				{
					return;
				}
				var names = settings.collections.Select(collection => collection.name).ToArray();
				int current = Math.Max(0, Array.IndexOf(names, binding.collectionName));
				int selected = EditorGUI.Popup(rect, current, names);
				if (selected == current)
					return;
				binding.collectionName = names[selected];
				settings.SaveToDisk();
			});

			table.AddColumn("Data Class", 140, 260, (rect, item) =>
			{
				if (SheetXCollectionSettings.IsAutomaticConfiguration(settings, item?.name))
				{
					EditorGUI.LabelField(rect, "GlobalConfigCollection");
					return;
				}
				if (!TryBinding(settings, sourceId, item, out var binding)
					|| binding.outputMode == SheetXSheetOutputMode.JsonOnly)
				{
					return;
				}
				if (binding.outputMode == SheetXSheetOutputMode.GeneratedDataClass)
				{
					string typeName = SheetXCollectionNaming.RowTypeName(item.name);
					EditorGUI.LabelField(rect, new GUIContent(typeName,
						settings.ResolveCollectionNamespace() + "." + typeName));
					return;
				}

				EnsureRowTypes();
				var selectedType = s_rowTypes.FirstOrDefault(
					type => string.Equals(type.AssemblyQualifiedName, binding.rowTypeName, StringComparison.Ordinal));
				string label = selectedType == null
					? (string.IsNullOrEmpty(binding.rowTypeName)
						? "<Select>"
						: "Missing: " + binding.rowTypeName)
					: selectedType.Name;
				string tooltip = selectedType == null ? label : selectedType.FullName;
				if (!EditorGUI.DropdownButton(rect, new GUIContent(label, tooltip), FocusType.Passive))
					return;
				new RowTypeDropdown(type =>
				{
					binding.rowTypeName = type?.AssemblyQualifiedName ?? "";
					settings.SaveToDisk();
				}).Show(rect);
			});
		}

		private static bool TryBinding(
			SheetXSettings settings,
			Func<string> sourceId,
			SheetPath item,
			out SheetXSheetBinding binding)
		{
			binding = null;
			if (settings == null || !settings.enableCollections
				|| item == null || !SheetXHelper.IsJsonSheet(item.name))
			{
				return false;
			}
			binding = SheetXCollectionSettings.GetOrCreateBinding(
				settings, sourceId?.Invoke() ?? "", item.name);
			return binding != null;
		}

		private static void EnsureRowTypes()
		{
			if (s_rowTypes != null)
				return;
			s_rowTypes = TypeCache.GetTypesWithAttribute<SheetXBindableAttribute>()
				.Where(type => SheetXRowType.Validate(type, out _))
				.OrderBy(type => type.FullName, StringComparer.Ordinal)
				.ToArray();
		}
	}
}
