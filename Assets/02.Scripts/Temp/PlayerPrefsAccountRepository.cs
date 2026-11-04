using UnityEngine;

public class PlayerPrefsAccountRepository : MonoBehaviour
{
    private const string KeyPrefix = "Account_";

    private string GetKeyID(string id)
    {
        return KeyPrefix + id;
    }
    
    public void Register(string id, string password)
    {
        PlayerPrefs.SetString(GetKeyID(id), password);
        PlayerPrefs.Save();
    }

    public bool Exists(string id)
    {
        return PlayerPrefs.HasKey(GetKeyID(id));
    }

    public bool CheckPassword(string id, string password)
    {
        return Exists(id) && PlayerPrefs.GetString(GetKeyID(id)) == password;
    }
}
