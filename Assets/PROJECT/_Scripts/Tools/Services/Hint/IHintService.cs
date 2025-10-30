using System.Collections.Generic;
using UnityEngine;

namespace Service
{
    public interface IHintService 
    {
        public void ShowHint(string localizationAction, List<CharacterAction> actions);
        void HideHint(string localizationAction);
        void HideAll();
        void ToggleView(bool value);
        Sprite GetHintSprite(CharacterAction action);

    }
}
  
