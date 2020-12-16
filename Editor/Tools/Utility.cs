﻿using UnityEngine;
using UnityEditor;
using InfinityTech.Runtime.Component;
//using Unity.Mathematics;

namespace InfinityTech.Editor.Tool
{
    public class Utility
    {
        [MenuItem("GameObject/EntityAction/RandomRotate", false, -1000)]
        public static void SetSelectEntityRandomRotate(MenuCommand menuCommand)
        {
            float StartTime = Time.realtimeSinceStartup;
            GameObject[] EntityList = Selection.gameObjects;
            for(int i = 0; i < EntityList.Length; i++)
            {
                GameObject Entity = EntityList[i];
                float RotateValue = Random.Range(-180, 180);
                Entity.transform.Rotate(RotateValue, RotateValue, RotateValue);
            }
            float EndTime = (Time.realtimeSinceStartup - StartTime) * 1000;
        }

        [MenuItem("GameObject/EntityAction/SpawnMatrixEntity", false, -1000)]
        public static void CreateMatrixEntity(MenuCommand menuCommand)
        {
            for(int z = 0; z < 10; z++)
            {
                for (int y = 0; y < 10; y++)
                {
                    for (int x = 0; x < 10; x++)
                    {
                        Vector3 Position = new Vector3(x * 5, y * 5, z * 5);

                        GameObject MeshEntity = new GameObject("MeshEntity");
                        MeshEntity.AddComponent<MeshComponent>();

                        MeshEntity.transform.position = Position;
                        GameObjectUtility.EnsureUniqueNameForSibling(MeshEntity);
                    }
                }
            }
        }

        [MenuItem("GameObject/EntityAction/RandomMaterial", false, -1000)]
        public static void SetSelectEntityRandomMaterial(MenuCommand menuCommand)
        {
            Material[] MaterialList = new Material[2];
            MaterialList[0] = Resources.Load<Material>("Materials/MeshBatchA");
            MaterialList[1] = Resources.Load<Material>("Materials/MeshBatchB");

            GameObject[] EntityList = Selection.gameObjects;
            for (int i = 0; i < EntityList.Length; i++)
            {
                GameObject Entity = EntityList[i];
                MeshRenderer meshRenderer = Entity.GetComponent<MeshRenderer>();
                MeshComponent meshComponent = Entity.GetComponent<MeshComponent>();

                int Index = Random.Range(-100, 100);
                Index = Mathf.Clamp(Index, 0, 1);

                if(meshRenderer != null)
                {
                    for (int j = 0; j < 1; j++)
                    {
                        meshRenderer.material = MaterialList[Index];
                    }
                }

                if(meshComponent != null)
                {
                    for (int j = 0; j < meshComponent.Materials.Length; j++)
                    {
                        meshComponent.Materials[j] = MaterialList[Index];
                    }
                }
            }
        }

        [MenuItem("Tool/EntityAction/RandomMaterial", false, -1000)]
        public static void SetRandomMaterial(MenuCommand menuCommand)
        {
            Material[] MaterialList = new Material[2];
            MaterialList[0] = Resources.Load<Material>("Materials/MeshBatchA");
            MaterialList[1] = Resources.Load<Material>("Materials/MeshBatchB");

            GameObject[] EntityList = GameObject.FindObjectsOfType<GameObject>();
  
            for (int i = 0; i < EntityList.Length; i++)
            {
                GameObject Entity = EntityList[i];
                MeshRenderer meshRenderer = Entity.GetComponent<MeshRenderer>();
                MeshComponent meshComponent = Entity.GetComponent<MeshComponent>();

                int Index = Random.Range(-100, 100);
                Index = Mathf.Clamp(Index, 0, 1);

                if(meshRenderer != null)
                {
                    for (int j = 0; j < 1; j++)
                    {
                        meshRenderer.material = MaterialList[Index];
                    }
                }

                if(meshComponent != null)
                {
                    for (int j = 0; j < meshComponent.Materials.Length; j++)
                    {
                        meshComponent.Materials[j] = MaterialList[Index];
                    }
                }
            }
        }
    }
}
