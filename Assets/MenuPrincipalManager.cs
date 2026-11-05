using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controla os painéis do menu, o botão global de som, a música de fundo
/// e os efeitos sonoros utilizados pelo menu e pela narrativa.
/// Mantenha este objeto ativo durante toda a execução do jogo.
/// </summary>
public class ControladorMenuInicial : MonoBehaviour
{
    [Header("Créditos")]
    [SerializeField] private GameObject painelCreditos;

    [Header("Sobre o jogo")]
    [SerializeField] private GameObject painelSobreOJogo;

    [Header("Botão de som")]
    [SerializeField] private Image imagemIconeSom;
    [SerializeField] private Sprite iconeSomLigado;
    [SerializeField] private Sprite iconeSomDesligado;
    [SerializeField] private bool iniciarComSom = true;

    [Header("Fontes de áudio")]
    [Tooltip("AudioSource usado exclusivamente para as músicas. Marque Loop e desmarque Play On Awake no Inspector.")]
    [SerializeField] private AudioSource fonteMusica;
    [Tooltip("AudioSource usado para os efeitos sonoros. Desmarque Loop e Play On Awake.")]
    [SerializeField] private AudioSource fonteEfeitos;

    [Header("Músicas")]
    [SerializeField] private AudioClip musicaMenu;
    [SerializeField] private AudioClip musicaNarrativa;
    [Tooltip("Opcional. Se ficar vazio, a música que já estiver tocando continua na reflexão final.")]
    [SerializeField] private AudioClip musicaReflexaoFinal;
    [Range(0f, 1f)]
    [SerializeField] private float volumeMusica = 0.35f;

    [Header("Efeitos sonoros")]
    [SerializeField] private AudioClip somClique;
    [SerializeField] private AudioClip somSelecao;
    [SerializeField] private AudioClip somContinuar;
    [Range(0f, 1f)]
    [SerializeField] private float volumeEfeitos = 0.8f;

    private bool somAtivo;

    private void Start()
    {
        somAtivo = iniciarComSom;

        if (painelCreditos != null)
            painelCreditos.SetActive(false);

        if (painelSobreOJogo != null)
            painelSobreOJogo.SetActive(false);

        ConfigurarFontesDeAudio();
        AplicarEstadoSom();
        TocarMusicaMenu();
    }

    private void ConfigurarFontesDeAudio()
    {
        if (fonteMusica != null)
        {
            fonteMusica.loop = true;
            fonteMusica.playOnAwake = false;
            fonteMusica.volume = volumeMusica;
        }
        else
        {
            Debug.LogWarning("ControladorMenuInicial: atribua um AudioSource ao campo Fonte Musica.");
        }

        if (fonteEfeitos != null)
        {
            fonteEfeitos.loop = false;
            fonteEfeitos.playOnAwake = false;
            fonteEfeitos.volume = volumeEfeitos;
        }
        else
        {
            Debug.LogWarning("ControladorMenuInicial: atribua um AudioSource ao campo Fonte Efeitos.");
        }
    }

    public void AlternarSom()
    {
        somAtivo = !somAtivo;
        AplicarEstadoSom();
    }

    private void AplicarEstadoSom()
    {
        // AudioListener.volume controla simultaneamente música e efeitos.
        AudioListener.volume = somAtivo ? 1f : 0f;

        if (imagemIconeSom != null)
        {
            Sprite sprite = somAtivo ? iconeSomLigado : iconeSomDesligado;
            if (sprite != null)
                imagemIconeSom.sprite = sprite;
        }
    }

    // ---------- Música de fundo ----------

    public void TocarMusicaMenu()
    {
        TrocarMusica(musicaMenu);
    }

    public void TocarMusicaNarrativa()
    {
        TrocarMusica(musicaNarrativa);
    }

    public void TocarMusicaReflexaoFinal()
    {
        // Uma faixa não configurada não interrompe a música atual.
        if (musicaReflexaoFinal != null)
            TrocarMusica(musicaReflexaoFinal);
    }

    private void TrocarMusica(AudioClip musica)
    {
        if (fonteMusica == null || musica == null)
            return;

        if (fonteMusica.clip == musica && fonteMusica.isPlaying)
            return;

        fonteMusica.Stop();
        fonteMusica.clip = musica;
        fonteMusica.loop = true;
        fonteMusica.volume = volumeMusica;
        fonteMusica.Play();
    }

    // ---------- Efeitos sonoros ----------

    public void TocarClique()
    {
        TocarEfeito(somClique);
    }

    public void TocarSelecao()
    {
        TocarEfeito(somSelecao);
    }

    public void TocarContinuar()
    {
        TocarEfeito(somContinuar);
    }

    private void TocarEfeito(AudioClip efeito)
    {
        if (!somAtivo || fonteEfeitos == null || efeito == null)
            return;

        fonteEfeitos.PlayOneShot(efeito, volumeEfeitos);
    }

    // ---------- Painéis do menu ----------

    public void AbrirCreditos()
    {
        TocarClique();
        if (painelCreditos != null)
            painelCreditos.SetActive(true);
        else
            Debug.LogWarning("ControladorMenuInicial: PainelCreditos não foi atribuído no Inspector.");
    }

    public void FecharCreditos()
    {
        TocarClique();
        if (painelCreditos != null)
            painelCreditos.SetActive(false);
    }

    public void AbrirSobreOJogo()
    {
        TocarClique();
        if (painelSobreOJogo != null)
            painelSobreOJogo.SetActive(true);
        else
            Debug.LogWarning("ControladorMenuInicial: PainelSobreOJogo não foi atribuído no Inspector.");
    }

    public void FecharSobreOJogo()
    {
        TocarClique();
        if (painelSobreOJogo != null)
            painelSobreOJogo.SetActive(false);
    }

    public void Sair()
    {
        TocarClique();
        Application.Quit();

#if UNITY_EDITOR
        // Permite testar o botão Sair durante o Play Mode no Editor.
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
