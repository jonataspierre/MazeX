using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public static SceneController instance;
    
    public string sceneName; // Nome da cena a ser carregada

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        LoadScene();
    }

    // Método para iniciar o carregamento assíncrono da cena
    public void LoadScene()
    {
        // Inicia a rotina de carregamento assíncrono
        StartCoroutine(LoadSceneAsync());
    }

    // Método que realiza o carregamento assíncrono
    private IEnumerator LoadSceneAsync()
    {
        // Cria uma operação de carregamento assíncrono
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        // Enquanto a cena não estiver completamente carregada, continua esperando
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // Torna a nova cena ativa
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
    }
}
