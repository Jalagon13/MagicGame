using UnityEngine;

namespace ProjectTinker
{
    public class InGameMenuInstantiateHandler : MonoBehaviour
    {
        private InGameMenuReferenceHolder _inGameMenuReferenceHolder;

        private void Awake()
        {
            _inGameMenuReferenceHolder = GetComponent<InGameMenuReferenceHolder>();
        }

        public GameObject InstantiateCustomMenu(GameObject customMenuPrefab)
        {
            GameObject customMenuUI = Instantiate(customMenuPrefab, transform);
            return customMenuUI;
        }

        public ChestMenuUI InstantiateChestMenu()
        {
            GameObject chestMenuUI = Instantiate(_inGameMenuReferenceHolder.ChestMenuPrefab, transform);

            return chestMenuUI.GetComponent<ChestMenuUI>();
        }

        public SpellbookInspectorMenuUI InstantiateWandInspectorMenu()
        {
            GameObject wandInspectorMenuUI = Instantiate(_inGameMenuReferenceHolder.WandInspectorMenuPrefab, transform);

            return wandInspectorMenuUI.GetComponent<SpellbookInspectorMenuUI>();
        }

        public NpcMenuUI InstantiateNpcMenu()
        {
            GameObject npcMenuUI = Instantiate(_inGameMenuReferenceHolder.NpcMenuPrefab, transform);

            return npcMenuUI.GetComponent<NpcMenuUI>();
        }
    }
}
