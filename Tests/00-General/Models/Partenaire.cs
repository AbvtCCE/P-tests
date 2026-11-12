using Newtonsoft.Json;

public class Partenaire
{
      private int? numeroPartenaire;

        private string? _email;

        public int? NumeroPartenaire
        {
            get => numeroPartenaire;
            set
            {
                numeroPartenaire = value;
            }
        }
        public string Email
        {
            get => _email!;
            set => _email = value;
        }
        public string? EmailCorrespondance { get; set; }
        public string? Nom { get; set; }

        public string? Prenom { get; set; }

        public string? LieuDeConsommation { get; set; }

        public string? Rue { get; set; }

        public string? NumRue { get; set; }

        public string? CodePostal { get; set; }

        public string? DescriptionHabitation { get; set; }

        public int? CompteContrat { get; set; }

        public int? numeroCompteur { get; set; }

        // Nouveaux champs
        public bool? FactureOuverte { get; set; }
        public bool? DemandeRallongement { get; set; }
        public bool? DemandeEpurement { get; set; }
        public bool? InscritSurLePortail { get; set; }
        public bool? DonneeConsommations { get; set; }
        public string? GarantieOrigine { get; set; }
        public string? ParticipeAuConcours { get; set; }
        public bool? AvertissementFactureParMail { get; set; }
        public bool? AvertissementAutre { get; set; }
        public string? Telephone { get; set; }
        public string? Mobile { get; set; }

        // Donnees compteur / produit / facture (sync portail, une ligne par compteur)
        public bool? CompteurIntelligent { get; set; }
        public bool? PortP1Actif { get; set; }
        public bool? CompteurActif { get; set; }
        public bool? FacturePayee { get; set; }
        public bool? BoutonsPaiementDisponibles { get; set; }

        public Partenaire() { }
        public Partenaire(int partnn_Num)
        {
            numeroPartenaire = partnn_Num;
        }
        public Partenaire(int numero, string email)
        {
            numeroPartenaire = numero;
            Email = email;
        }

        public Partenaire(int partn_Num, int CptCtt_num)
        {
            this.numeroPartenaire = partn_Num;
            AssignGmail();
            this.CompteContrat = CptCtt_num;

        }
        public Partenaire(int partn_Num, int CptCtt_num, string mail)
        {
            this.numeroPartenaire = partn_Num;
            this.Email = mail;
            this.CompteContrat = CptCtt_num;

        }

        public Partenaire(string numero, string nom, string prenom, string email, string lc, string rue, string numRue, string codePostal, string descHab, string CmptCtt, string mntDerFact, string nbFacture)
        {
            this.NumeroPartenaire = Convert.ToInt32(numero);
            this.Nom = nom;
            this.Prenom = prenom;
            this.Email = email;
            this.LieuDeConsommation = lc;
            this.Rue = rue;
            this.NumRue = numRue;
            this.CodePostal = codePostal;
            this.DescriptionHabitation = descHab;
            this.CompteContrat = Convert.ToInt32(CmptCtt);
        }



        public void AssignGmail()
        {
            if (this.Email != "test4gre+" + this.NumeroPartenaire + "@gmail.com")
            {
                this.Email = "test4gre+" + this.NumeroPartenaire + "@gmail.com";
            }
        }

        // Méthode pour afficher les infos du partnenaire
        public override string ToString()
        {
            return $"Partenaire :{NumeroPartenaire}  \n" +
                   $"- Nom : {Nom}\n" +
                   $"- Prénom : {Prenom}\n" +
                   $"- Email : {Email}\n" +
                   $"- Lieu de consommation : {LieuDeConsommation}\n" +
                   $"- Rue : {Rue} {NumRue}, {CodePostal}\n" +
                   $"- Description habitation : {DescriptionHabitation}\n" +
                   $"- Compte de contrats : {CompteContrat}\n" +
                   $"- Facture ouverte : {FactureOuverte?.ToString() ?? "Non renseigné"}\n" +
                   $"- Demande rallongement : {DemandeRallongement?.ToString() ?? "Non renseigné"}\n" +
                   $"- Demande épurement : {DemandeEpurement?.ToString() ?? "Non renseigné"}\n" +
                   $"- Inscrit sur le portail : {InscritSurLePortail?.ToString() ?? "Non renseigné"}\n" +
                   $"- Donnée consommations : {DonneeConsommations?.ToString() ?? "Non renseigné"}\n" +
                   $"- Garantie origine : {GarantieOrigine}";
        }


        public void ToStringDump()
        {
            var props = typeof(Partenaire).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            Console.WriteLine("Détails du client :");
            foreach (var prop in props)
            {
                var value = prop.GetValue(this);
                Console.WriteLine($"- {prop.Name} : {value ?? "Non renseigné"}");
            }
        }



        public static class ClientValidator
        {
            public static List<string> Valider(Partenaire partn, List<string> champsObligatoires)
            {
                var erreurs = new List<string>();

                foreach (var champ in champsObligatoires)
                {
                    var prop = typeof(Partenaire).GetProperty(champ);
                    if (prop == null)
                    {
                        erreurs.Add($"Champ inconnu : {champ}");
                        continue;
                    }

                    var valeur = prop.GetValue(partn);
                    if (valeur == null || string.IsNullOrWhiteSpace(valeur.ToString()))
                    {
                        erreurs.Add($"Le champ '{champ}' est obligatoire.");
                    }
                }

                return erreurs;
            }



        }



}
