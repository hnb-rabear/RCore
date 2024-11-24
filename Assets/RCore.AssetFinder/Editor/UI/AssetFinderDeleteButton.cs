﻿using System;
using UnityEditor;
using UnityEngine;
using VirtueSky.AssetFinder.Editor;


public class AssetFinderDeleteButton
{
    public string warningMessage;
    public string confirmMessage;
    public GUIContent deleteLabel;
    public bool hasConfirm;

    public bool Draw(Action onConfirmDelete)
    {
        GUILayout.BeginHorizontal();
        {
            EditorGUILayout.HelpBox(warningMessage, MessageType.Warning);
            GUILayout.BeginVertical();
            {
                GUILayout.Space(2f);
                hasConfirm = GUILayout.Toggle(hasConfirm, confirmMessage);
                EditorGUI.BeginDisabledGroup(!hasConfirm);
                {
                    GUI2.BackgroundColor(() =>
                    {
                        if (GUILayout.Button(deleteLabel, EditorStyles.miniButton))
                        {
                            hasConfirm = false;
                            onConfirmDelete();
                            GUIUtility.ExitGUI();
                        }
                    }, GUI2.darkRed, 0.8f);
                }
                EditorGUI.EndDisabledGroup();
            }
            GUILayout.EndVertical();
        }
        GUILayout.EndHorizontal();
        return false;
    }
}