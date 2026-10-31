namespace LostTemple
{
    /// <summary>
    /// Journal de l'explorateur : une page s'écrit à la fin de chaque salle. Raconte l'histoire
    /// et glisse un indice sur la relique secrète de la salle suivante.
    /// </summary>
    public static class Journal
    {
        public static readonly string[] Pages =
        {
            "Jour 1. La dalle s'est refermée derrière moi : impossible de ressortir par là où je suis entré. Le pont suspendu n'a cédé qu'à la boule d'or… Les anciens savaient que l'équilibre ouvre les portes.\n\n<i>On raconte que le labyrinthe garde un trésor pour ceux qui osent tout renverser.</i>",
            "Jour 1, plus tard. L'aiguille magique a senti le nord de la Terre elle-même. Ce temple n'a pas été construit au hasard : il est aligné sur les étoiles.\n\n<i>Dans la salle des statues, la pierre aime qu'on la secoue.</i>",
            "Jour 2. Les trois statues regardent enfin leurs symboles. J'ai entendu un ronflement derrière la porte… quelque chose dort là-dessous.\n\n<i>Le monstre déteste le silence, mais son trésor répond aux cris.</i>",
            "Jour 2, soir. Le monstre n'est plus que poussière. Ma voix a suffi. Ces murs réagissent au souffle, à la lumière, au toucher… comme s'ils étaient vivants.\n\n<i>Dans la cachette, une main posée longtemps révèle ce que les gardiens cachent.</i>",
            "Jour 3. Les gardiens momifiés ne voient pas ce qui se cache dans l'ombre de ma main. Les runes de la cachette brillent encore.\n\n<i>Au-dessus du gouffre, seule l'obscurité totale montre le chemin des braves.</i>",
            "Jour 3, nuit. Un pont de cristal, invisible à la lumière ordinaire ! Ma lanterne faiblit, mais je sens la présence de l'artefact tout près.\n\n<i>Le Coyote garde un dernier secret pour ceux qui retournent la situation.</i>",
            "Dernier jour. L'esprit Coyote m'a confié sa clé, et le gardien des ténèbres est tombé. L'artefact est à moi… ou bien est-ce moi qui appartiens désormais au temple ?\n\nFIN.",
        };

        /// <summary>Geste de la relique secrète de chaque salle (indice donné par la page précédente).</summary>
        public static readonly SecretGesture[] Secrets =
        {
            SecretGesture.FlipFaceDown,   // salle 1 (pas d'indice préalable : à découvrir)
            SecretGesture.FlipFaceDown,   // salle 2 : « tout renverser »
            SecretGesture.Shake,          // salle 3 : « la pierre aime qu'on la secoue »
            SecretGesture.Shout,          // salle 4 : « répond aux cris »
            SecretGesture.CoverLong,      // salle 5 : « une main posée longtemps »
            SecretGesture.Darkness,       // salle 6 : « l'obscurité totale »
            SecretGesture.FlipFaceDown,   // salle 7 : « retournent la situation »
        };
    }
}
