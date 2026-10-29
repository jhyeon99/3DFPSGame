using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_PlayerWeapons : MonoBehaviour
{
    [SerializeField] private Player _player;
    [SerializeField] private PlayerGunFire _playerGunFire;
    [SerializeField] private PlayerBombFire _playerBombFire;
    [SerializeField] private TextMeshProUGUI _weaponModeText;
    [SerializeField] private Image _weaponModeImage;
    [SerializeField] private Image _crosshairImage;
    [SerializeField] private TextMeshProUGUI _bulletText;
    [SerializeField] private TextMeshProUGUI _bombText;
    
    [SerializeField] private Sprite _weaponSpriteNormal;
    [SerializeField] private Sprite _weaponSpriteSniper;
    [SerializeField] private Sprite _crosshairSpriteNormal;
    [SerializeField] private Sprite _crosshairSpriteSniper;

    private void OnEnable()
    {
        _player.OnWeaponChanged += UpdateWeaponModeText;
        _player.OnWeaponChanged += UpdateWeaponModeImage;
        _player.OnWeaponChanged += UpdateCrosshairImage;
        _playerGunFire.OnBulletChanged += UpdateBulletText;
        _playerBombFire.OnBombChanged += UpdateBombText;
    }

    private void Start()
    {
        // 이벤트는 변경 시에만 오므로, 시작 시점의 현재 값을 읽어 초기 표시를 맞춘다.
        UpdateWeaponModeText(_player.WeaponMode);
        UpdateWeaponModeImage(_player.WeaponMode);
        UpdateCrosshairImage(_player.WeaponMode);
        UpdateBulletText(_playerGunFire.CurrentBullet, _playerGunFire.MaxBullet);
        UpdateBombText(_playerBombFire.RemainBomb);
    }

    private void OnDisable()
    {
        _player.OnWeaponChanged -= UpdateWeaponModeText;
        _player.OnWeaponChanged -= UpdateWeaponModeImage;
        _player.OnWeaponChanged -= UpdateCrosshairImage;
        _playerGunFire.OnBulletChanged -= UpdateBulletText;
        _playerBombFire.OnBombChanged -= UpdateBombText;
    }
    
    private void UpdateWeaponModeText(WeaponMode weaponMode)
    {
        if (weaponMode == WeaponMode.Normal)
        {
            _weaponModeText.text = "일반 모드";
        }
        else if (weaponMode == WeaponMode.Sniper)
        {
            _weaponModeText.text = "저격 모드";
        }
    }
    
    private void UpdateWeaponModeImage(WeaponMode weaponMode)
    {
        if (weaponMode == WeaponMode.Normal)
        {
            _weaponModeImage.sprite = _weaponSpriteNormal;
        }
        else if (weaponMode == WeaponMode.Sniper)
        {
            _weaponModeImage.sprite = _weaponSpriteSniper;
        }
    }
    
    private void UpdateCrosshairImage(WeaponMode weaponMode)
    {
        if (weaponMode == WeaponMode.Normal)
        {
            _crosshairImage.sprite = _crosshairSpriteNormal;
        }
        else if (weaponMode == WeaponMode.Sniper)
        {
            _crosshairImage.sprite = _crosshairSpriteSniper;
        }
    }

    private void UpdateBulletText(int currentBullet, int maxBullet)
    {
        _bulletText.text = $"{currentBullet} / {maxBullet}";
    }

    private void UpdateBombText(int currentBomb)
    {
        _bombText.text = $"폭탄 {currentBomb}";
    }
}
