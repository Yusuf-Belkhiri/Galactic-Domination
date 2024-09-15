using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Aircraft
{
    public class AircraftArea : MonoBehaviour
    {
        [Serializable]
        public class MaterialSpawnPoint
        {
            public Transform spawnPoint;
            //public MaterialName _MaterialItemName;
            public MaterialItem materialItem;
        }
        
        
        
    #region FIELDS


        public List<AircraftAgent> AircraftAgents { get; private set; }
        public bool _trainingMode;
        
        [field:SerializeField]public List<MaterialSpawnPoint> MaterialSpawnPoints { get; private set; }        // REMOVE VISIBILITY
        [SerializeField] private float _timeToResetMaterials = 8f;

        [SerializeField] private MaterialItem[] _materialItemsPrefabs;


        [SerializeField] private Transform _materialSpawnPointsContainer;       // just to rotate the material items
    #endregion
        

    #region METHODS

        private void Awake()
        {
            AircraftAgents = GetComponentsInChildren<AircraftAgent>().ToList();

            MaterialSpawnPoints = new List<MaterialSpawnPoint>();

            var materialSpawnPointsList = _materialSpawnPointsContainer.GetComponentsInChildren<Transform>();
            for (int i = 1; i < materialSpawnPointsList.Length; i++)
            {
                MaterialSpawnPoints.Add(new MaterialSpawnPoint{spawnPoint = materialSpawnPointsList[i], materialItem = null});
            }
            
            /* foreach (var materialSpawnPoint in GameObject.FindGameObjectsWithTag("MaterialSpawnPoint"))
            {
                MaterialSpawnPoints.Add(new MaterialSpawnPoint{spawnPoint = materialSpawnPoint.transform, materialItem = null});
            }*/
            
            InvokeRepeating(nameof(ResetMaterials), 1, _timeToResetMaterials);
        }
        
        [SerializeField] private AircraftPlayer _player;
        private void ResetMaterials()
        {
            if(GameManager.Instance.GameState != GameState.Playing)
                return;
            foreach (var materialSpawnPoint in MaterialSpawnPoints)
            {

                if (materialSpawnPoint.materialItem != null)        // (materialSpawnPoint._MaterialItemName != MaterialType.NoMaterial)
                {
                    Destroy(materialSpawnPoint.materialItem.transform.gameObject);
                    materialSpawnPoint.materialItem = null;
                }
                
                int randomIndex = Random.Range(0, _materialItemsPrefabs.Length);
                if (randomIndex == -1)
                {
                    return;
                }
                
                //var randomMaterialItem = _materialItemsPrefabs[randomIndex];
                materialSpawnPoint.materialItem = Instantiate(_materialItemsPrefabs[randomIndex], materialSpawnPoint.spawnPoint.position, materialSpawnPoint.spawnPoint.rotation, _materialSpawnPointsContainer);
                //Instantiate(randomMaterialItem, materialSpawnPoint.spawnPoint.position, materialSpawnPoint.spawnPoint.rotation, materialSpawnPoint.spawnPoint);

                if (materialSpawnPoint.materialItem == null)
                {
                    return;
                }
                materialSpawnPoint.materialItem.transform.GetComponentInChildren<Canvas>().gameObject.SetActive(materialSpawnPoint.materialItem.MaterialType == _player.RequiredMaterialType);
            }
        }
        

        // public void RemoveMaterial(Transform spawnPoint)
        // {
        //     var materialSpawnPoint = MaterialSpawnPoints.Find(m => m._spawnPoint == spawnPoint);
        //     materialSpawnPoint._material = null;
        // }

        
    #endregion
    }
}
