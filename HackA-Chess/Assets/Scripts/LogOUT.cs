using System.Threading.Tasks;
using Assets.Scripts;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LogOUT : MonoBehaviour
{
    [SerializeField] Button dangxuat;

    public async void OnClick()
    {
        try
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopBGM();   
            }
            await NetworkClient.Instance.SendAsync("LOGOUT|");
            SceneManager.LoadScene("Login");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Lỗi khi logout: " + ex.Message);
        }
        finally
        {
            dangxuat.interactable = true;
        }
    }
}
