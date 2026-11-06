using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ControladorNarrativa : MonoBehaviour
{
    [System.Serializable]
    private class RegistroEscolha
    {
        public string nomeCenario;
        public string nomeQuadro;
        public int numeroOpcao;
        public string textoOpcao;
        public string consequencia;
        public string feedback;
        public int pontuacao;

        public RegistroEscolha(
            string cenario,
            string quadro,
            int numero,
            string opcao,
            string efeito,
            string retorno,
            int pontos)
        {
            nomeCenario = cenario;
            nomeQuadro = quadro;
            numeroOpcao = numero;
            textoOpcao = opcao;
            consequencia = efeito;
            feedback = retorno;
            pontuacao = pontos;
        }
    }

    [System.Serializable]
    private class FaixaFeedbackFinal
    {
        [Tooltip("Pontuação mínima inclusiva para esta faixa.")]
        public int pontuacaoMinima;
        [Tooltip("Pontuação máxima inclusiva para esta faixa.")]
        public int pontuacaoMaxima;
        [TextArea(3, 8)]
        public string feedback;
    }

    [Header("Menu Inicial")]
    [Tooltip("Painel inicial que contém o botão Jogar. Deixe vazio se não houver menu inicial nesta cena.")]
    [SerializeField] private GameObject painelMenuInicial;

    [Header("Áudio")]
    [Tooltip("Arraste o objeto que contém o script ControladorMenuInicial. Ele controla música, efeitos e o botão de som.")]
    [SerializeField] private ControladorMenuInicial controladorMenuInicial;

    [Header("Lista de cenários")]
    [SerializeField] private List<DadosCenario> cenarios = new List<DadosCenario>();

    [Header("Elementos da narrativa")]
    [SerializeField] private Image imagemQuadro;
    [SerializeField] private TMP_Text tituloQuadro;
    [SerializeField] private TMP_Text textoQuadro;

    [Header("Imagens de conclusão")]
    [Tooltip("Imagem exibida quando um cenário termina, antes de voltar à seleção ou ir à reflexão final.")]
    [SerializeField] private Sprite imagemConclusaoCenario;
    [Tooltip("Imagem exibida na tela da reflexão final do jogo.")]
    [SerializeField] private Sprite imagemReflexaoFinal;

    [Header("Escolhas")]
    [SerializeField] private GameObject painelEscolhas;
    [SerializeField] private Button[] botoesOpcoes;

    [Header("Continuar")]
    [SerializeField] private Button botaoContinuar;

    [Header("Voltar para a seleção")]
    [SerializeField] private Button botaoVoltar;

    [Header("Tela de seleção")]
    [SerializeField] private GameObject painelSelecaoCenarios;
    [SerializeField] private GameObject painelNarrativa;
    [SerializeField] private Button[] botoesSelecaoCenarios;
    [SerializeField] private Image[] imagensCapaCenarios;
    [SerializeField] private GameObject[] iconesCadeado;
    [SerializeField] private TMP_Text[] titulosCenarios;

    [Header("Painel exclusivo de conclusão de cenário")]
    [Tooltip("Painel independente usado somente ao terminar cada cenário. Não é o painel da reflexão final geral.")]
    [SerializeField] private GameObject painelConclusaoCenario;

    [Header("Área da consequência")]
    [Tooltip("Título separado do texto da consequência. Exemplo: Consequência.")]
    [SerializeField] private TMP_Text tituloConsequenciaCenario;
    [Tooltip("Texto da consequência cadastrada na opção escolhida pelo jogador.")]
    [SerializeField] private TMP_Text textoConsequenciaCenario;

    [Header("Área do feedback da opção")]
    [Tooltip("Título separado do texto do feedback. Exemplo: Feedback.")]
    [SerializeField] private TMP_Text tituloFeedbackCenario;
    [Tooltip("Feedback educativo/reflexivo cadastrado na opção escolhida pelo jogador.")]
    [SerializeField] private TMP_Text textoFeedbackCenario;

    [Tooltip("ScrollRect opcional que envolve as áreas de consequência e feedback do painel de conclusão.")]
    [SerializeField] private ScrollRect scrollConclusaoCenario;
    [Tooltip("Imagem opcional dentro do painel de conclusão do cenário.")]
    [SerializeField] private Image imagemPainelConclusaoCenario;
    [Tooltip("Botão Continuar do painel exclusivo de conclusão. Retorna à seleção ou abre a reflexão geral se for o último cenário.")]
    [SerializeField] private Button botaoContinuarConclusaoCenario;

    [Header("Tela de reflexão final geral")]
    [SerializeField] private GameObject painelReflexaoFinal;
    [SerializeField] private TMP_Text tituloReflexaoFinal;

    [Tooltip("Arraste aqui o componente ScrollRect do Scroll View da reflexão final.")]
    [SerializeField] private ScrollRect scrollReflexaoFinal;

    [Tooltip("Arraste aqui o TextMeshPro que fica dentro de Scroll View > Viewport > Content.")]
    [SerializeField] private TMP_Text textoReflexaoFinal;

    [SerializeField] private Button botaoVoltarMenuFinal;

    [Header("Feedback final configurável") ]
    [Tooltip("Configure as faixas de pontuação e o texto de retorno no Inspector. A pontuação total é a soma das opções escolhidas.")]
    [SerializeField] private List<FaixaFeedbackFinal> faixasFeedbackFinal = new List<FaixaFeedbackFinal>();

    [Tooltip("Texto usado se nenhuma faixa configurada corresponder à pontuação total.")]
    [TextArea(3, 8)]
    [SerializeField] private string feedbackPadraoFinal = "Sua jornada convida a continuar ouvindo, respeitando a autonomia e evitando estereótipos relacionados à idade.";

    private bool[] cenariosDesbloqueados;
    private int cenarioAtual;
    private int quadroAtual;
    private int proximoQuadroEscolhido = -1;
    private int opcaoSelecionada = -1;
    private bool aguardandoProximoCenario;
    private bool jogoFinalizado;
    // false = consequência; true = feedback. As duas etapas usam o mesmo painel.
    private bool exibindoFeedbackConclusao;

    // Respostas da tentativa atual e respostas das histórias concluídas.
    private readonly List<RegistroEscolha> respostasJogador = new List<RegistroEscolha>();
    private readonly Dictionary<int, List<RegistroEscolha>> respostasPorCenario =
        new Dictionary<int, List<RegistroEscolha>>();
    private List<RegistroEscolha> respostasCenarioAtual = new List<RegistroEscolha>();
    private bool respostasCenarioAtualSalvas;

    private void Start()
    {
        if (cenarios == null || cenarios.Count == 0)
        {
            Debug.LogError("ControladorNarrativa: a lista Cenários está vazia. Adicione os assets DadosCenario no Inspector.");
            return;
        }

        if (painelSelecaoCenarios == null)
            Debug.LogError("ControladorNarrativa: arraste PainelSelecaoCenarios para o campo correspondente no Inspector.");

        if (painelNarrativa == null)
            Debug.LogError("ControladorNarrativa: arraste PainelNarrativa para o campo correspondente no Inspector.");

        if (botaoContinuar != null)
        {
            botaoContinuar.onClick.RemoveListener(Continuar);
            botaoContinuar.onClick.AddListener(Continuar);
        }
        else
        {
            Debug.LogError("ControladorNarrativa: o campo BotaoContinuar não foi preenchido no Inspector.");
        }

        if (botaoVoltar != null)
        {
            botaoVoltar.onClick.RemoveListener(VoltarParaSelecao);
            botaoVoltar.onClick.AddListener(VoltarParaSelecao);
        }

        cenariosDesbloqueados = new bool[cenarios.Count];
        cenariosDesbloqueados[0] = true; // O primeiro cenário começa desbloqueado.

        ConfigurarBotoesSelecao();

        // O controlador deve ficar em um GameObject ativo que não seja um destes painéis.
        if (painelNarrativa != null)
            painelNarrativa.SetActive(false);

        if (painelMenuInicial != null)
        {
            painelMenuInicial.SetActive(true);
            if (painelSelecaoCenarios != null)
                painelSelecaoCenarios.SetActive(false);
        }
        else if (painelSelecaoCenarios != null)
        {
            // Sem menu inicial configurado, abre a seleção diretamente.
            painelSelecaoCenarios.SetActive(true);
        }

        if (painelConclusaoCenario != null)
            painelConclusaoCenario.SetActive(false);
        else
            Debug.LogWarning("PainelConclusaoCenario não foi atribuído no Inspector.");

        if (tituloConsequenciaCenario == null || textoConsequenciaCenario == null ||
            tituloFeedbackCenario == null || textoFeedbackCenario == null)
        {
            Debug.LogWarning("Associe no Inspector os quatro componentes TMP do painel: título/texto da consequência e título/texto do feedback.");
        }

        if (painelReflexaoFinal != null)
            painelReflexaoFinal.SetActive(false);

        if (botaoContinuarConclusaoCenario != null)
        {
            botaoContinuarConclusaoCenario.onClick.RemoveListener(ContinuarAposConclusaoCenario);
            botaoContinuarConclusaoCenario.onClick.AddListener(ContinuarAposConclusaoCenario);
        }
        else
        {
            Debug.LogWarning("BotaoContinuarConclusaoCenario não foi atribuído. O painel de conclusão não terá botão de avanço.");
        }

        if (botaoVoltarMenuFinal != null)
        {
            botaoVoltarMenuFinal.onClick.RemoveListener(
                VoltarAoMenuInicialDaReflexao
            );

            botaoVoltarMenuFinal.onClick.AddListener(
                VoltarAoMenuInicialDaReflexao
            );
        }

        AtualizarTelaSelecao();
    }

    private void ConfigurarBotoesSelecao()
    {
        if (botoesSelecaoCenarios == null)
        {
            Debug.LogError("ControladorNarrativa: o array BotoesSelecaoCenarios não foi configurado.");
            return;
        }

        for (int i = 0; i < botoesSelecaoCenarios.Length; i++)
        {
            Button botao = botoesSelecaoCenarios[i];
            if (botao == null)
            {
                Debug.LogWarning("ControladorNarrativa: o botão de seleção no índice " + i + " está vazio.");
                continue;
            }

            int indice = i;
            botao.onClick.RemoveAllListeners();
            botao.onClick.AddListener(() => IniciarCenario(indice));
        }
    }

    // Vincule este método ao OnClick do botão Jogar do menu inicial.
    public void AbrirSelecaoCenarios()
    {
        TocarClique();
        TocarMusicaMenu();

        if (painelMenuInicial != null)
            painelMenuInicial.SetActive(false);

        if (painelNarrativa != null)
            painelNarrativa.SetActive(false);
        if (painelConclusaoCenario != null)
            painelConclusaoCenario.SetActive(false);
        if (painelReflexaoFinal != null)
            painelReflexaoFinal.SetActive(false);

        if (painelSelecaoCenarios != null)
            painelSelecaoCenarios.SetActive(true);
        else
            Debug.LogError("Não é possível abrir a seleção: PainelSelecaoCenarios não foi atribuído.");

        AtualizarTelaSelecao();
    }

    private void AtualizarTelaSelecao()
    {
        if (botoesSelecaoCenarios == null)
            return;

        for (int i = 0; i < botoesSelecaoCenarios.Length; i++)
        {
            Button botao = botoesSelecaoCenarios[i];
            if (botao == null)
                continue;

            if (cenarios == null || i >= cenarios.Count || cenarios[i] == null)
            {
                botao.gameObject.SetActive(false);
                continue;
            }

            bool desbloqueado = cenariosDesbloqueados != null &&
                                i < cenariosDesbloqueados.Length &&
                                cenariosDesbloqueados[i];

            botao.gameObject.SetActive(true);
            botao.interactable = desbloqueado;

            if (titulosCenarios != null &&
                i < titulosCenarios.Length &&
                titulosCenarios[i] != null)
            {
                titulosCenarios[i].text = cenarios[i].nomeCenario;
            }

            if (imagensCapaCenarios != null && i < imagensCapaCenarios.Length && imagensCapaCenarios[i] != null)
            {
                Image imagem = imagensCapaCenarios[i];
                imagem.sprite = cenarios[i].imagemCapa;
                imagem.color = desbloqueado
                    ? Color.white
                    : new Color(0.25f, 0.25f, 0.25f, 1f);
            }

            if (iconesCadeado != null && i < iconesCadeado.Length && iconesCadeado[i] != null)
                iconesCadeado[i].SetActive(!desbloqueado);
        }
    }

    private void IniciarCenario(int indice)
    {
        TocarClique();
        TocarMusicaNarrativa();

        if (cenarios == null || indice < 0 || indice >= cenarios.Count)
            return;

        if (cenariosDesbloqueados == null || indice >= cenariosDesbloqueados.Length || !cenariosDesbloqueados[indice])
            return;

        DadosCenario cenario = cenarios[indice];
        if (cenario == null || cenario.quadros == null || cenario.quadros.Length == 0)
        {
            Debug.LogError("O cenário no índice " + indice + " não possui quadros configurados.");
            return;
        }

        cenarioAtual = indice;
        quadroAtual = 0;
        opcaoSelecionada = -1;
        proximoQuadroEscolhido = -1;
        aguardandoProximoCenario = false;
        jogoFinalizado = false;
        exibindoFeedbackConclusao = false;

        // Só substituímos as respostas anteriores se esta nova tentativa for concluída.
        // Assim, abandonar uma repetição não apaga os dados de uma história já concluída.
        respostasCenarioAtual = new List<RegistroEscolha>();
        respostasCenarioAtualSalvas = false;

        if (painelMenuInicial != null)
            painelMenuInicial.SetActive(false);
        if (painelSelecaoCenarios != null)
            painelSelecaoCenarios.SetActive(false);
        if (painelConclusaoCenario != null)
            painelConclusaoCenario.SetActive(false);
        if (painelReflexaoFinal != null)
            painelReflexaoFinal.SetActive(false);
        if (painelNarrativa != null)
            painelNarrativa.SetActive(true);

        MostrarQuadro();
    }

    private void VoltarParaSelecao()
    {
        if (jogoFinalizado || aguardandoProximoCenario)
            return;
        
        TocarClique();
        TocarMusicaMenu();

        if (painelNarrativa != null)
            painelNarrativa.SetActive(false);
        if (painelSelecaoCenarios != null)
            painelSelecaoCenarios.SetActive(true);

        AtualizarTelaSelecao();
    }

    private void MostrarQuadro()
    {
        if (cenarioAtual < 0 || cenarioAtual >= cenarios.Count)
        {
            Debug.LogError("Índice do cenário inválido: " + cenarioAtual);
            return;
        }

        DadosCenario cenario = cenarios[cenarioAtual];
        if (cenario == null || cenario.quadros == null)
        {
            Debug.LogError("O cenário ou seu array de quadros não foi configurado.");
            return;
        }

        // Um índice igual ao tamanho do array significa fim do cenário.
        if (quadroAtual == cenario.quadros.Length)
        {
            FinalizarCenario();
            return;
        }

        if (quadroAtual < 0 || quadroAtual > cenario.quadros.Length)
        {
            Debug.LogError("Índice de quadro inválido (" + quadroAtual + ") no cenário '" + cenario.nomeCenario + "'. Verifique os valores Proximo Quadro nas opções.");
            return;
        }

        DadosCenario.Quadro quadro = cenario.quadros[quadroAtual];
        if (quadro == null)
        {
            Debug.LogError("O quadro no índice " + quadroAtual + " do cenário '" + cenario.nomeCenario + "' está vazio.");
            return;
        }

        if (tituloQuadro != null)
            tituloQuadro.text = quadro.titulo;
        if (textoQuadro != null)
            textoQuadro.text = quadro.texto;

        if (imagemQuadro != null)
        {
            imagemQuadro.sprite = quadro.imagem;
            imagemQuadro.gameObject.SetActive(quadro.imagem != null);
        }

        opcaoSelecionada = -1;
        proximoQuadroEscolhido = -1;
        aguardandoProximoCenario = false;

        int totalOpcoes = quadro.opcoes == null ? 0 : quadro.opcoes.Length;
        bool quadroTemEscolha = quadro.exigeEscolha && totalOpcoes > 0;

        if (quadro.exigeEscolha && totalOpcoes == 0)
            Debug.LogWarning("O quadro '" + quadro.titulo + "' exige escolha, mas não tem opções. Ele será tratado como um quadro de narrativa.");

        if (botaoContinuar != null)
        {
            botaoContinuar.gameObject.SetActive(!quadroTemEscolha);
            botaoContinuar.interactable = !quadroTemEscolha;
            DefinirTextoBotao("Continuar");
        }

        // O botão Voltar leva à seleção e não aparece durante a escolha.
        if (botaoVoltar != null)
            botaoVoltar.gameObject.SetActive(!quadroTemEscolha);

        if (painelEscolhas != null)
            painelEscolhas.SetActive(quadroTemEscolha);

        if (botoesOpcoes == null)
            return;

        for (int i = 0; i < botoesOpcoes.Length; i++)
        {
            Button botao = botoesOpcoes[i];
            if (botao == null)
                continue;

            botao.onClick.RemoveAllListeners();

            if (i < totalOpcoes && quadroTemEscolha)
            {
                botao.gameObject.SetActive(true);
                botao.interactable = true;

                TMP_Text textoBotao = botao.GetComponentInChildren<TMP_Text>(true);
                if (textoBotao != null)
                    textoBotao.text = quadro.opcoes[i].textoOpcao;

                int indice = i;
                botao.onClick.AddListener(() => SelecionarOpcao(indice));
            }
            else
            {
                botao.gameObject.SetActive(false);
            }
        }
    }

    private void SelecionarOpcao(int indice)
    {
        DadosCenario cenario = cenarios[cenarioAtual];
        DadosCenario.Quadro quadro = cenario.quadros[quadroAtual];

        if (quadro.opcoes == null || indice < 0 || indice >= quadro.opcoes.Length)
            return;

        TocarSelecao();

        // Clicar na opção já selecionada desmarca a escolha.
        if (opcaoSelecionada == indice)
        {
            opcaoSelecionada = -1;
            proximoQuadroEscolhido = -1;

            if (textoQuadro != null)
                textoQuadro.text = quadro.texto;
            if (botaoContinuar != null)
            {
                botaoContinuar.gameObject.SetActive(false);
                botaoContinuar.interactable = false;
            }
            if (botaoVoltar != null)
                botaoVoltar.gameObject.SetActive(false);

            if (botoesOpcoes != null)
            {
                foreach (Button botao in botoesOpcoes)
                    if (botao != null) botao.interactable = true;
            }
            return;
        }

        opcaoSelecionada = indice;
        DadosCenario.Opcao opcao = quadro.opcoes[indice];
        proximoQuadroEscolhido = opcao.proximoQuadro;

        // A consequência narrativa completa será exibida no quadro indicado por
        // opcao.proximoQuadro quando o jogador clicar em Continuar.
        // Não substituímos o texto do quadro inicial nesta etapa.

        if (botaoContinuar != null)
        {
            botaoContinuar.gameObject.SetActive(true);
            botaoContinuar.interactable = true;
        }

        // O botão Voltar fica oculto enquanto a resposta estiver selecionada.
        if (botaoVoltar != null)
            botaoVoltar.gameObject.SetActive(false);

        // Deixa as opções interativas para que o jogador possa trocar de resposta.
        if (botoesOpcoes != null)
        {
            foreach (Button botao in botoesOpcoes)
                if (botao != null) botao.interactable = true;
        }
    }

    private void Continuar()
    {
        if (jogoFinalizado || cenarios == null || cenarios.Count == 0)
            return;

        TocarContinuar();

        if (aguardandoProximoCenario)
        {
            // Segurança caso algum botão antigo ainda esteja ligado a Continuar.
            ContinuarAposConclusaoCenario();
            return;
        }

        DadosCenario cenario = cenarios[cenarioAtual];
        if (cenario == null || cenario.quadros == null || quadroAtual < 0 || quadroAtual >= cenario.quadros.Length)
        {
            Debug.LogError("Não foi possível avançar: cenário ou quadro inválido.");
            return;
        }

        DadosCenario.Quadro quadro = cenario.quadros[quadroAtual];
        if (quadro == null)
        {
            Debug.LogError("O quadro atual está vazio.");
            return;
        }

        if (opcaoSelecionada >= 0)
        {
            DadosCenario.Opcao opcao = quadro.opcoes[opcaoSelecionada];

            // O valor pode apontar para um quadro válido ou para quadros.Length (fim da história).
            if (proximoQuadroEscolhido < 0 || proximoQuadroEscolhido > cenario.quadros.Length)
            {
                Debug.LogError(
                    "A opção '" + opcao.textoOpcao + "' no quadro '" + quadro.titulo +
                    "' tem Proximo Quadro inválido: " + proximoQuadroEscolhido +
                    ". Use um índice entre 0 e " + cenario.quadros.Length +
                    " (" + cenario.quadros.Length + " significa fim do cenário)."
                );
                return;
            }

            // A pontuação vem da configuração individual da opção no Inspector.
            // Assim, a ordem visual das opções não altera a avaliação.
            int pontuacao = opcao.pontuacao;
            RegistroEscolha registro = new RegistroEscolha(
                cenario.nomeCenario,
                quadro.titulo,
                opcaoSelecionada + 1,
                opcao.textoOpcao,
                opcao.textoConsequencia,
                opcao.textoFeedback,
                pontuacao
            );
            respostasCenarioAtual.Add(registro);
            quadroAtual = proximoQuadroEscolhido;
        }
        else
        {
            // Quadros sem escolha podem seguir em sequência ou apontar para
            // um quadro específico (por exemplo, para convergir os caminhos).
            if (quadro.definirProximoQuadroManualmente)
            {
                int destino = quadro.proximoQuadroSemEscolha;

                if (destino < 0 || destino > cenario.quadros.Length)
                {
                    Debug.LogError(
                        "O quadro '" + quadro.titulo + "' tem Proximo Quadro Sem Escolha inválido: " + destino +
                        ". Use um índice entre 0 e " + cenario.quadros.Length +
                        " (o tamanho do array significa fim do cenário)."
                    );
                    return;
                }

                quadroAtual = destino;
            }
            else
            {
                // Com a opção desmarcada, mantém o comportamento antigo.
                quadroAtual++;
            }
        }

        MostrarQuadro();
    }

    private void FinalizarCenario()
    {
        aguardandoProximoCenario = true;

        // Salva as escolhas apenas quando a história é concluída.
        if (!respostasCenarioAtualSalvas)
        {
            respostasPorCenario[cenarioAtual] = new List<RegistroEscolha>(respostasCenarioAtual);
            ReconstruirRespostasJogador();
            respostasCenarioAtualSalvas = true;
        }

        if (painelEscolhas != null)
            painelEscolhas.SetActive(false);
        if (painelNarrativa != null)
            painelNarrativa.SetActive(false);
        if (painelReflexaoFinal != null)
            painelReflexaoFinal.SetActive(false);

        if (painelConclusaoCenario == null)
        {
            Debug.LogError("PainelConclusaoCenario não foi atribuído no Inspector. Crie e associe o painel exclusivo para o fim dos cenários.");
            return;
        }

        DadosCenario dados = cenarios[cenarioAtual];
        Sprite imagem = dados != null ? dados.imagemConclusao : null;
        if (imagem == null)
            imagem = imagemConclusaoCenario;

        if (imagemPainelConclusaoCenario != null)
        {
            imagemPainelConclusaoCenario.sprite = imagem;
            imagemPainelConclusaoCenario.gameObject.SetActive(imagem != null);
        }

        // Prepara os dois conteúdos, mas apresenta apenas a consequência nesta primeira etapa.
        // O feedback só será exibido depois que o jogador clicar em Continuar.
        if (tituloConsequenciaCenario != null)
        {
            tituloConsequenciaCenario.text = "Consequência";
            tituloConsequenciaCenario.alignment = TextAlignmentOptions.Center;
            tituloConsequenciaCenario.verticalAlignment = VerticalAlignmentOptions.Middle;
        }

        if (textoConsequenciaCenario != null)
        {
            textoConsequenciaCenario.text = GerarConsequenciasCenarioAtual();
            textoConsequenciaCenario.alignment = TextAlignmentOptions.Center;
            textoConsequenciaCenario.verticalAlignment = VerticalAlignmentOptions.Middle;
        }

        if (tituloFeedbackCenario != null)
        {
            tituloFeedbackCenario.text = "Feedback";
            tituloFeedbackCenario.alignment = TextAlignmentOptions.Center;
            tituloFeedbackCenario.verticalAlignment = VerticalAlignmentOptions.Middle;
        }

        if (textoFeedbackCenario != null)
        {
            textoFeedbackCenario.text = GerarFeedbackCenarioAtual();
            textoFeedbackCenario.alignment = TextAlignmentOptions.Center;
            textoFeedbackCenario.verticalAlignment = VerticalAlignmentOptions.Middle;
        }

        exibindoFeedbackConclusao = false;
        AtualizarEtapaPainelConclusao();
        painelConclusaoCenario.SetActive(true);

        if (botaoContinuarConclusaoCenario != null)
        {
            botaoContinuarConclusaoCenario.gameObject.SetActive(true);
            botaoContinuarConclusaoCenario.interactable = true;
            TMP_Text label = botaoContinuarConclusaoCenario.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = "Continuar";
        }

        Canvas.ForceUpdateCanvases();
        if (scrollConclusaoCenario != null)
            scrollConclusaoCenario.verticalNormalizedPosition = 1f;
    }

    private string GerarConsequenciasCenarioAtual()
    {
        StringBuilder sb = new StringBuilder();

        if (respostasCenarioAtual == null || respostasCenarioAtual.Count == 0)
            return "Nenhuma escolha foi registrada neste cenário.";

        // Junta somente os textos de consequência. O título é exibido
        // separadamente pelo componente tituloConsequenciaCenario.
        for (int i = 0; i < respostasCenarioAtual.Count; i++)
        {
            RegistroEscolha registro = respostasCenarioAtual[i];
            if (string.IsNullOrWhiteSpace(registro.consequencia))
                continue;

            if (sb.Length > 0)
                sb.Append("\n\n");

            sb.Append(registro.consequencia.Trim());
        }

        if (sb.Length == 0)
            return "Nenhum texto de consequência foi cadastrado para as opções escolhidas.";

        return sb.ToString();
    }

    private string GerarFeedbackCenarioAtual()
    {
        StringBuilder sb = new StringBuilder();

        if (respostasCenarioAtual == null || respostasCenarioAtual.Count == 0)
            return "Nenhum feedback foi registrado neste cenário.";

        // Junta somente os feedbacks. O título é exibido separadamente
        // pelo componente tituloFeedbackCenario.
        for (int i = 0; i < respostasCenarioAtual.Count; i++)
        {
            RegistroEscolha registro = respostasCenarioAtual[i];
            if (string.IsNullOrWhiteSpace(registro.feedback))
                continue;

            if (sb.Length > 0)
                sb.Append("\n\n");

            sb.Append(registro.feedback.Trim());
        }

        if (sb.Length == 0)
            return "Nenhum feedback foi cadastrado para as opções escolhidas.";

        return sb.ToString();
    }

    private void AtualizarEtapaPainelConclusao()
    {
        // Cada etapa mostra somente seu título e seu texto.
        if (tituloConsequenciaCenario != null)
            tituloConsequenciaCenario.gameObject.SetActive(!exibindoFeedbackConclusao);

        if (textoConsequenciaCenario != null)
            textoConsequenciaCenario.gameObject.SetActive(!exibindoFeedbackConclusao);

        if (tituloFeedbackCenario != null)
            tituloFeedbackCenario.gameObject.SetActive(exibindoFeedbackConclusao);

        if (textoFeedbackCenario != null)
            textoFeedbackCenario.gameObject.SetActive(exibindoFeedbackConclusao);
    }

    private void ContinuarAposConclusaoCenario()
    {
        if (!aguardandoProximoCenario || jogoFinalizado)
            return;
    
        TocarContinuar();

        // Primeiro clique: troca a consequência pelo feedback, sem fechar o painel.
        if (!exibindoFeedbackConclusao)
        {
            exibindoFeedbackConclusao = true;
            AtualizarEtapaPainelConclusao();

            if (botaoContinuarConclusaoCenario != null)
            {
                TMP_Text label = botaoContinuarConclusaoCenario.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                    label.text = cenarioAtual < cenarios.Count - 1
                        ? "Voltar à seleção"
                        : "Ver reflexão final";
            }

            Canvas.ForceUpdateCanvases();
            if (scrollConclusaoCenario != null)
                scrollConclusaoCenario.verticalNormalizedPosition = 1f;
            return;
        }

        // Segundo clique: encerra o painel depois de exibir o feedback.
        if (painelConclusaoCenario != null)
            painelConclusaoCenario.SetActive(false);

        if (cenarioAtual < cenarios.Count - 1)
        {
            // Concluir o cenário atual desbloqueia o próximo da sequência.
            int proximo = cenarioAtual + 1;
            if (cenariosDesbloqueados != null && proximo < cenariosDesbloqueados.Length)
                cenariosDesbloqueados[proximo] = true;

            aguardandoProximoCenario = false;
            exibindoFeedbackConclusao = false;
            if (painelSelecaoCenarios != null)
                painelSelecaoCenarios.SetActive(true);
            TocarMusicaMenu();
            AtualizarTelaSelecao();
        }
        else
        {
            // Após o feedback do último cenário, abre a reflexão geral do jogo.
            FinalizarJogo();
        }
    }

    private void ReconstruirRespostasJogador()
    {
        respostasJogador.Clear();

        // Mantém uma ordem previsível: cenário 1, 2, 3...
        for (int i = 0; i < cenarios.Count; i++)
        {
            List<RegistroEscolha> respostas;
            if (respostasPorCenario.TryGetValue(i, out respostas))
                respostasJogador.AddRange(respostas);
        }
    }

    private void FinalizarJogo()
    {
        jogoFinalizado = true;
        if (controladorMenuInicial != null)
            controladorMenuInicial.TocarMusicaReflexaoFinal();
        aguardandoProximoCenario = false;

        if (painelNarrativa != null) painelNarrativa.SetActive(false);
        if (painelSelecaoCenarios != null) painelSelecaoCenarios.SetActive(false);
        if (painelMenuInicial != null) painelMenuInicial.SetActive(false);
        if (painelEscolhas != null) painelEscolhas.SetActive(false);
        if (painelConclusaoCenario != null) painelConclusaoCenario.SetActive(false);

        if (painelReflexaoFinal != null)
        {
            if (tituloReflexaoFinal != null)
                tituloReflexaoFinal.text = "Reflexão final";

            if (textoReflexaoFinal != null)
            {
                textoReflexaoFinal.text = GerarFeedbackFinal();
                textoReflexaoFinal.alignment = TextAlignmentOptions.Center;
                textoReflexaoFinal.verticalAlignment = VerticalAlignmentOptions.Middle;
            }

            painelReflexaoFinal.SetActive(true);
            if (botaoVoltarMenuFinal != null)
            {
                botaoVoltarMenuFinal.gameObject.SetActive(true);
                botaoVoltarMenuFinal.interactable = true;
            }

            Canvas.ForceUpdateCanvases();
            if (scrollReflexaoFinal != null)
                scrollReflexaoFinal.verticalNormalizedPosition = 1f;
        }
        else
        {
            Debug.LogError("PainelReflexaoFinal não foi atribuído no Inspector. Configure o painel exclusivo de reflexão geral.");
        }

        RegistrarRespostasNoConsole();
    }

    private string GerarFeedbackFinal()
    {
        StringBuilder sb = new StringBuilder();
        int totalPontos = 0;

        // Soma os pontos das escolhas registradas durante todos os cenários.
        foreach (RegistroEscolha resposta in respostasJogador)
            totalPontos += resposta.pontuacao;

        // Seleciona o feedback geral correspondente à faixa de pontuação.
        string feedbackGeral = feedbackPadraoFinal;

        if (faixasFeedbackFinal != null)
        {
            foreach (FaixaFeedbackFinal faixa in faixasFeedbackFinal)
            {
                if (faixa != null &&
                    totalPontos >= faixa.pontuacaoMinima &&
                    totalPontos <= faixa.pontuacaoMaxima)
                {
                    feedbackGeral = faixa.feedback;
                    break;
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(feedbackGeral))
            sb.Append(feedbackGeral.Trim());

        // Mantém apenas o aviso educativo. O resumo das escolhas e dos
        // feedbacks individuais por cenário foi removido desta tela.
        sb.Append("\n\nAviso: este feedback tem finalidade exclusivamente educativa e reflexiva. " +
                  "Ele não representa uma avaliação psicológica, não mede empatia, " +
                  "não constitui diagnóstico e não deve ser interpretado como julgamento pessoal.");

        return sb.ToString();
    }
        
    private void TocarClique()
    {
        if (controladorMenuInicial != null)
            controladorMenuInicial.TocarClique();
    }

    private void TocarSelecao()
    {
        if (controladorMenuInicial != null)
            controladorMenuInicial.TocarSelecao();
    }

    private void TocarContinuar()
    {
        if (controladorMenuInicial != null)
            controladorMenuInicial.TocarContinuar();
    }

    private void TocarMusicaMenu()
    {
        if (controladorMenuInicial != null)
            controladorMenuInicial.TocarMusicaMenu();
    }

    private void TocarMusicaNarrativa()
    {
        if (controladorMenuInicial != null)
            controladorMenuInicial.TocarMusicaNarrativa();
    }

    private void RegistrarRespostasNoConsole()
    {
        Debug.Log("===== REGISTRO DAS ESCOLHAS =====");
        foreach (RegistroEscolha registro in respostasJogador)
        {
            Debug.Log(
                "Cenário: " + registro.nomeCenario +
                "\nQuadro: " + registro.nomeQuadro +
                "\nOpção: " + registro.numeroOpcao +
                "\nTexto: " + registro.textoOpcao +
                "\nConsequência: " + registro.consequencia +
                "\nFeedback: " + registro.feedback +
                "\nIndicador reflexivo: " + registro.pontuacao
            );
        }
        Debug.Log("===== FIM DO REGISTRO =====");
    }

    private void DefinirTextoBotao(string texto)
    {
        if (botaoContinuar == null)
            return;

        TMP_Text textoBotao = botaoContinuar.GetComponentInChildren<TMP_Text>(true);
        if (textoBotao != null)
            textoBotao.text = texto;
    }

    public void VoltarParaMenuInicial()
    {
        TocarClique();
        TocarMusicaMenu();

        if (painelSelecaoCenarios != null)
            painelSelecaoCenarios.SetActive(false);

        if (painelMenuInicial != null)
            painelMenuInicial.SetActive(true);
    }

    public void VoltarAoMenuInicialDaReflexao()
    {
        TocarClique();
        TocarMusicaMenu();

        if (painelReflexaoFinal != null)
            painelReflexaoFinal.SetActive(false);
        if (painelConclusaoCenario != null)
            painelConclusaoCenario.SetActive(false);

        if (painelNarrativa != null)
            painelNarrativa.SetActive(false);

        if (painelSelecaoCenarios != null)
            painelSelecaoCenarios.SetActive(false);

        if (painelMenuInicial != null)
            painelMenuInicial.SetActive(true);
        else
            Debug.LogError(
                "PainelMenuInicial não foi atribuído no Inspector."
            );
    }
}