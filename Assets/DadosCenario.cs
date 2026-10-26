using UnityEngine;

[CreateAssetMenu(
    fileName = "NovoCenario",
    menuName = "Jogo/Cenario Narrativo"
)]
public class DadosCenario : ScriptableObject
{
    [Header("Tela de Seleção")]
    public Sprite imagemCapa;

    [Header("Informações do Cenário")]
    public string nomeCenario;

    [Header("Quadros Narrativos")]
    public Quadro[] quadros;

    [System.Serializable]
    public class Opcao
    {
        [TextArea(2, 4)]
        public string textoOpcao;

        [TextArea(2, 5)]
        public string textoConsequencia;

        [Tooltip("Índice do quadro que será exibido quando o jogador confirmar esta opção. Os índices começam em 0. Use o tamanho do array para encerrar o cenário.")]
        public int proximoQuadro;
    }

    [System.Serializable]
    public class Quadro
    {
        public string titulo;

        [TextArea(3, 8)]
        public string texto;

        public Sprite imagem;

        public Opcao[] opcoes;

        public bool exigeEscolha;

        [Header("Fluxo após este quadro")]
        [Tooltip("Marque para escolher manualmente o próximo quadro após clicar em Continuar. Desmarcado: segue para o próximo índice normalmente.")]
        public bool definirProximoQuadroManualmente;

        [Tooltip("Índice do próximo quadro quando a opção acima estiver marcada. Índices começam em 0. Use o tamanho do array para finalizar o cenário.")]
        public int proximoQuadroSemEscolha;
    }
}
