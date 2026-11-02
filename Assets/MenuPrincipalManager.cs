using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controla as ações do menu inicial: som, créditos e saída do jogo.
/// Pode ser colocado no Canvas ou em um GameObject separado que permaneça ativo.
/// </summary>
public class ControladorMenuInicial : MonoBehaviour
{
    [Header("Créditos")]
    [SerializeField] private GameObject painelCreditos;

    [Header("Sobre o jogo")]
    [SerializeField] private GameObject painelSobreOJogo;

    [Header("Som")]
    [SerializeField] private Image imagemIconeSom;
    [SerializeField] private Sprite iconeSomLigado;
    [SerializeField] private Sprite iconeSomDesligado;
    [SerializeField] private bool iniciarComSom = true;

    private bool somAtivo;

    private void Start()
    {
        somAtivo = iniciarComSom;

        if (painelCreditos != null)
            painelCreditos.SetActive(false);

        if (painelSobreOJogo != null)
            painelSobreOJogo.SetActive(false);

        AplicarEstadoSom();
    }

    public void AlternarSom()
    {
        somAtivo = !somAtivo;
        AplicarEstadoSom();
    }

    private void AplicarEstadoSom()
    {
        // Controla o volume global de áudio do jogo.
        AudioListener.volume = somAtivo ? 1f : 0f;

        // Atualiza o ícone para mostrar se o som está ligado ou desligado.
        if (imagemIconeSom != null)
        {
            Sprite sprite = somAtivo ? iconeSomLigado : iconeSomDesligado;
            if (sprite != null)
                imagemIconeSom.sprite = sprite;
        }
    }

    public void AbrirCreditos()
    {
        if (painelCreditos != null)
            painelCreditos.SetActive(true);
        else
            Debug.LogWarning("ControladorMenuInicial: PainelCreditos não foi atribuído no Inspector.");
    }

    public void FecharCreditos()
    {
        if (painelCreditos != null)
            painelCreditos.SetActive(false);
    }

    public void AbrirSobreOJogo()
    {
        if (painelSobreOJogo != null)
            painelSobreOJogo.SetActive(true);
    }

    public void FecharSobreOJogo()
    {
        if (painelSobreOJogo != null)
            painelSobreOJogo.SetActive(false);
    }

    public void Sair()
    {
        Application.Quit();

#if UNITY_EDITOR
        // Permite testar o botão Sair durante o Play Mode no Editor.
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}