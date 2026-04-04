using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace NeonSerpent.UI
{
    /// <summary>
    /// A single skin slot tile in the Shop grid. Displays the skin preview image,
    /// display name, coin price (when locked), an equipped badge, and a locked overlay.
    /// The button calls back to ShopUI via the <c>onSelect</c> delegate.
    /// </summary>
    public class SkinSlotUI : MonoBehaviour
    {
        [SerializeField] private Image    _previewImage;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _priceText;
        [SerializeField] private Button   _button;
        [SerializeField] private GameObject _equippedBadge;
        [SerializeField] private GameObject _lockedOverlay;

        /// <summary>
        /// Populates the slot with skin data and wires the selection button.
        /// </summary>
        /// <param name="skin">The skin definition to display.</param>
        /// <param name="isUnlocked">True if the player already owns this skin.</param>
        /// <param name="isEquipped">True if this skin is the one currently equipped.</param>
        /// <param name="onSelect">Callback invoked with the skin ID when the button is pressed.</param>
        public void Populate(ShopUI.SkinItem skin, bool isUnlocked, bool isEquipped, Action<string> onSelect)
        {
            if (_previewImage != null)
            {
                _previewImage.sprite  = skin.PreviewSprite;
                _previewImage.enabled = skin.PreviewSprite != null;
            }

            if (_nameText  != null) _nameText.text  = skin.DisplayName;

            // Price label: only meaningful when the skin is locked.
            if (_priceText != null)
            {
                _priceText.text            = $"{skin.CoinCost:N0}";
                _priceText.gameObject.SetActive(!isUnlocked);
            }

            if (_equippedBadge != null) _equippedBadge.SetActive(isEquipped);
            if (_lockedOverlay != null) _lockedOverlay.SetActive(!isUnlocked);

            // Re-wire the button: remove previous listeners first to prevent stacking.
            _button?.onClick.RemoveAllListeners();
            _button?.onClick.AddListener(() => onSelect?.Invoke(skin.Id));
        }
    }
}
