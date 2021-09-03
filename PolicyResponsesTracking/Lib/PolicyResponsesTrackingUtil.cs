using Oecd.GoogleAnalyticsUtility.Lib;
using Oecd.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using static Oecd.GoogleAnalyticsUtility.GoogleAnalyticsAPI;

namespace Oecd.PolicyResponsesTrackingUtil.Lib
{
    public static class PolicyResponsesTrackingUtil
    {
        private static readonly string _readURL = "read.oecd-ilibrary.org/view/?ref=";
        private static readonly string _oecdURL = "oecd.org/coronavirus/policy-responses/";
        // id for oecd url is prececding by a '-', with Length 8 And Contains Alphabetic And Number
        private static readonly string _oecdPattern = @"(?<=-)([A-Za-z0-9]{8})(?=[\/\.\?&])";

        // At the very begining of the covid hub, some url didn't contain any id.
        // Here we have a list of key/value (= "url without id" / "realted id") extracted from the kv3 policy response report (no use to update this list: all PR url has got an id now)
        // good to know: for non latin languages (ie: Japanese, Russian), the url is the same as english one. English is choosen as they were published as the begining and the others have been removed from the list.
        private static readonly List<KeyValuePair<string, string>> _listStaticOecdUrls = new()
        {
            new KeyValuePair<string, string>("coronavirus/policy-responses/a-debt-standstill-for-the-poorest-countries-how-much-is-at-stake", "462eabd8"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/a-systemic-resilience-approach-to-dealing-with-covid-19-and-future-shocks", "36a5bdfb"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/access-to-covid-19-vaccines-global-approaches-in-a-global-crisis", "c6a18370"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/accroitre-la-resilience-face-a-la-pandemie-de-covid-19-le-role-des-centres-de-gouvernement", "7c177686"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/administration-fiscale-la-resilience-numerique-dans-le-contexte-du-covid-19", "addaac0c"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/administration-fiscale-risques-lies-a-la-pandemie-de-covid-19-en-matiere-de-protection-de-la-vie-privee-de-confidentialite-des-donnees-et-de-fraude", "3dc8210d"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/adult-learning-and-covid-19-how-much-informal-and-non-formal-learning-are-workers-missing", "56a96569"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/africa-s-response-to-covid-19-what-roles-for-trade-manufacturing-and-intellectual-property", "73d0dfaf"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/an-assessment-of-the-impact-of-covid-19-on-job-and-skills-demand-using-online-job-vacancy-data", "20fff09e"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/apoio-as-pessoas-e-empresas-para-lidar-com-o-virus-covid-19-opcoes-de-respostas-imediatas-para-o-emprego-e-as-politicas-sociais", "3771a5e3"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/apoyar-a-las-personas-y-las-empresas-para-combatir-el-covid-19-opciones-para-una-respuesta-inmediata-en-materia-de-empleo-y-politica-social", "4752b583"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/apporter-une-reponse-englobant-toutes-les-composantes-de-la-societe-face-aux-consequences-de-la-crise-du-covid-19-sur-la-sante-mentale", "f4d9703f"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/aprender-a-distancia-cuando-las-escuelas-cierran-cuan-bien-estan-preparados-los-estudiantes-y-las-escuelas-ensenanzas-de-pisa", "4ead1e4b"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/beyond-containment-health-systems-responses-to-covid-19-in-the-oecd", "6ab740c0"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/biodiversite-et-reponse-economique-au-covid-19-assurer-une-reprise-verte-et-resiliente", "0c20417e"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/biodiversity-and-the-economic-response-to-covid-19-ensuring-a-green-and-resilient-recovery", "d98b5a09"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/building-a-coherent-response-for-a-sustainable-post-covid-19-recovery", "d67eab68"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/building-back-better-a-sustainable-resilient-recovery-after-covid-19", "52b869f5"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/building-resilience-to-the-covid-19-pandemic-the-role-of-centres-of-government", "883d2961"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/business-dynamism-during-the-covid-19-pandemic-which-policies-for-an-inclusive-recovery", "f08af011"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/capacity-for-remote-working-can-affect-lockdown-costs-differently-across-places", "0e85740e"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/cities-policy-responses", "fd1053ff"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/combatir-el-efecto-de-covid-19-en-los-ninos", "34c42a7c"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/combatting-covid-19-disinformation-on-online-platforms", "d854ec48"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/combatting-covid-19-s-effect-on-children", "2e1f3b2f"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/combattre-la-desinformation-sur-le-covid-19-sur-les-plateformes-en-ligne", "e17b4532"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/comment-communiquer-efficacement-sur-les-migrations-et-l-integration-dans-le-contexte-du-covid-19", "0aac467b"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/connecting-businesses-and-consumers-during-covid-19-trade-in-parcels", "d18de131"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/consequences-environnementales-a-long-terme-du-covid-19", "3f6e0c70"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/contribution-des-medecins-et-des-infirmiers-migrants-a-la-lutte-contre-la-crise-du-covid-19-dans-les-pays-de-l-ocde", "63ff0143"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/contribution-of-migrant-doctors-and-nurses-to-tackling-covid-19-crisis-in-oecd-countries", "2f7bace2"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/conventions-fiscales-et-impact-de-la-crise-du-covid-19-analyse-du-secretariat-de-l-ocde", "f856f704"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/coronavirus-covid-19-sme-policy-responses", "04440101"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/coronavirus-covid-19-vaccines-for-developing-countries-an-equal-shot-at-recovery", "6b0771e6"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/corporate-sector-vulnerabilities-during-the-covid-19-outbreak-assessment-and-policy-responses", "a6e670ea"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/covid-19-and-a-new-resilient-infrastructure-landscape", "d40a19e3"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/covid-19-and-africa-socio-economic-implications-and-policy-responses", "96e1b282"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/covid-19-and-fiscal-relations-across-levels-of-government", "ab438b9f"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/covid-19-and-global-capital-flows", "2dc69002"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/covid-19-and-global-food-systems", "aeb1434b"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/covid-19-and-global-value-chains-policy-options-to-build-more-resilient-production-networks", "04934ef4"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/covid-19-and-greening-the-economies-of-eastern-europe-the-caucasus-and-central-asia", "40f4d34f"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/covid-19-and-international-trade-issues-and-actions", "494da2fa"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/covid-19-and-key-workers-what-role-do-migrants-play-in-your-region", "42847cb9"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/covid-19-and-multilateral-fisheries-management", "cc1214fe"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/covid-19-and-responsible-business-conduct", "02150b06"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/covid-19-and-the-aviation-industry-impact-and-policy-responses", "26d521c1"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/covid-19-and-the-food-and-agriculture-sector-issues-and-policy-responses", "a23f764b"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/covid-19-and-the-low-carbon-transition-impacts-and-possible-policy-responses", "749738fc"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/covid-19-and-the-retail-sector-impact-and-policy-responses", "371d7599"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/covid-19-crises-and-fragility", "2f17a262"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/covid-19-crisis-in-the-mena-region-impact-on-gender-equality-and-policy-responses", "ee4cd4f4"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/covid-19-crisis-response-in-asean-member-states", "02f828a2"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/covid-19-crisis-response-in-central-asia", "5305f172"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/covid-19-crisis-response-in-eastern-partner-countries", "7759afa3"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/covid-19-crisis-response-in-mena-countries", "4b366396"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/covid-19-crisis-response-in-south-east-european-economies", "c1aacb5a"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/covid-19-crisis-y-fragilidad", "8ea010df"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/covid-19-dans-la-region-mena-impact-sur-les-inegalites-de-genre-et-reponses-apportees-en-soutien-aux-femmes", "f7da7585"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/covid-19-e-as-relacoes-fiscais-entre-os-niveis-de-governo", "2bb04f6c"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/covid-19-e-comercio-internacional-temas-e-acoes", "db62abed"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/covid-19-e-o-setor-agroalimentar-questoes-e-respostas", "3827aa9f"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/covid-19-en-america-latina-y-el-caribe-consecuencias-socioeconomicas-y-prioridades-de-politica", "26a07844"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/covid-19-en-america-latina-y-el-caribe-panorama-de-las-respuestas-de-los-gobiernos-a-la-crisis", "7d9f7a2b"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/covid-19-en-amerique-latine-et-dans-les-caraibes-un-apercu-des-reponses-gouvernementales-a-la-crise", "ae45a602"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/covid-19-et-travailleurs-essentiels-quel-role-jouent-les-immigres-dans-votre-region", "c3e86dd1"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/covid-19-in-emerging-asia-regional-socio-economic-implications-and-policy-priorities", "da08f00f"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/covid-19-in-latin-america-and-the-caribbean-an-overview-of-government-responses-to-the-crisis", "0a2dee41"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/covid-19-in-latin-america-and-the-caribbean-regional-socio-economic-implications-and-policy-priorities", "93a64fde"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/covid-19-international-mobility-and-trade-in-services-the-road-to-recovery", "ec716823"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/covid-19-na-america-latina-e-no-caribe-uma-visao-geral-das-respostas-dos-governos-a-crise", "9290226e"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/covid-19-na-regiao-da-america-latina-e-caribe-implicacoes-sociais-e-economicas-e-politicas-prioritarias", "433b9d11"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/covid-19-pandemic-towards-a-blue-recovery-in-small-island-developing-states", "241271b7"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/covid-19-protecting-people-and-societies", "e5c9de1a"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/covid-19-proteger-a-las-personas-y-las-sociedades", "56ebae97"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/covid-19-und-der-internationale-handel-herausforderungen-und-massnahmen", "0fc6ca7d"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/covid-19-und-verantwortungsvolles-unternehmerisches-handeln", "9d5eb69f"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/covid-19-y-el-sector-minorista-impacto-y-respuestas-politicas", "886315e6"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/covid-19-y-la-industria-aerea-impacto-y-respuestas-politicas", "d8615a3a"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/crowdsourcing-sti-policy-solutions-to-covid-19", "c4f057b3"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/culture-shock-covid-19-and-the-cultural-and-creative-sectors", "08da9e0e"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/dealing-with-digital-security-risk-during-the-coronavirus-covid-19-crisis", "c9d3fe8e"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/del-confinamiento-a-la-recuperacion-respuestas-medioambientales-a-la-pandemia-del-covid-19", "2b7d712b"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/depistage-du-covid-19-comment-utiliser-au-mieux-les-differents-tests", "1850e93e"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/developing-countries-and-development-co-operation-what-is-at-stake", "50e97915"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/du-confinement-a-la-reprise-les-reponses-environnementales-a-la-pandemie-de-covid-19", "88ddfed3"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/e-commerce-in-the-time-of-covid-19", "3a2b78e8"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/educacion-profesional-tecnica-ept-en-tiempos-de-crisis-sentar-las-bases-para-sistemas-de-ept-resilientes", "2e6eda90"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/education-and-covid-19-focusing-on-the-long-term-impact-of-school-closures", "2cea926e"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/education-et-covid-19-les-repercussions-a-long-terme-de-la-fermeture-des-ecoles", "7ab43642"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/education-responses-to-covid-19-embracing-digital-learning-and-online-collaboration", "d75eb0e8"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/effets-du-covid-19-sur-la-consommation-d-alcool-et-mesures-prises-pour-prevenir-la-consommation-nocive-d-alcool", "600e9145"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/effets-positifs-potentiels-du-teletravail-sur-la-productivite-a-l-ere-post-covid-19-quelles-politiques-publiques-peuvent-aider-a-leur-concretisation", "a43c958f"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/el-covid-19-y-la-conducta-empresarial-responsable", "b2efc058"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/enhancing-public-trust-in-covid-19-vaccination-the-role-of-governments", "eae0ec5a"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/enquete-des-risques-qui-comptent-2020-les-effets-a-long-terme-du-covid-19", "99fe0cc4"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/ensuring-data-privacy-as-we-battle-covid-19", "36c2f31e"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/environmental-health-and-strengthening-resilience-to-pandemics", "73784e04"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/equity-injections-and-unforeseen-state-ownership-of-enterprises-during-the-covid-19-crisis", "3bdb26f0"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/eviter-la-corruption-et-les-pots-de-vin-dans-les-reponses-au-covid-19-et-dans-les-mesures-de-relance", "2766c04d"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/filtrage-des-investissements-pendant-la-crise-de-la-covid-19-et-au-dela", "8c27deef"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/fisheries-aquaculture-and-covid-19-issues-and-policy-responses", "a2aa15de"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/flattening-the-covid-19-peak-containment-and-mitigation-policies", "e96a4226"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/food-supply-chains-and-covid-19-impacts-and-policy-lessons", "71b57aea"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/foreign-direct-investment-flows-in-the-time-of-covid-19", "a2fa20c4"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/from-containment-to-recovery-environmental-responses-to-the-covid-19-pandemic", "92c49c5c"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/from-pandemic-to-recovery-local-employment-and-economic-development", "879d2913"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/garantir-a-privacidade-de-dados-na-luta-contra-a-covid-19", "30f8c591"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/garantizar-la-privacidad-de-datos-mientras-luchamos-contra-el-covid-19", "49dc1537"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/gerer-les-migrations-internationales-dans-le-contexte-du-covid-19", "50199083"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/getting-goods-across-borders-in-times-of-covid-19", "972ada7a"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/global-financial-markets-policy-responses-to-covid-19", "2d98c7e0"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/global-value-chains-efficiency-and-risks-in-the-context-of-covid-19", "67c75fdc"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/government-financial-management-and-reporting-in-times-of-crisis", "3f87c7d8"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/government-support-and-the-covid-19-pandemic", "cb8ca170"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/greater-harmonisation-of-clinical-trial-regulations-would-help-the-fight-against-covid-19", "732e1c5c"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/green-budgeting-and-tax-policy-tools-to-support-a-green-recovery", "bd02ea23"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/guidance-on-the-transfer-pricing-implications-of-the-covid-19-pandemic", "731a59b0"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/guide-sur-les-consequences-de-la-pandemie-de-covid-19-en-matiere-de-prix-de-transfert", "dba1f40e"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/housing-amid-covid-19-policy-responses-and-challenges", "cfdc08a8"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/how-best-to-communicate-on-migration-and-integration-in-the-context-of-covid-19", "813bddfb"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/how-will-covid-19-reshape-science-technology-and-innovation", "2332334d"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/impacto-territorial-de-la-covid-19-gestionar-la-crisis-en-todos-los-niveles-de-gobierno", "7d27f7d9"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/implications-de-la-crise-du-coronavirus-pour-le-developpement-rural", "7bb4ae6d"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/independent-fiscal-institutions-promoting-fiscal-transparency-and-accountability-during-the-coronavirus-covid-19-pandemic", "d853f8be"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/initiative-de-l-ocde-pour-une-mobilite-internationale-sans-danger-pendant-la-pandemie-de-covid-19-comprenant-le-cadre", "712c2462"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/innovation-development-and-covid-19-challenges-opportunities-and-ways-forward", "0c976158"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/insolvency-and-debt-overhang-following-the-covid-19-outbreak-assessment-of-risks-and-policy-responses", "7806f078"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/insurance-coverage-and-covid-19", "8d22a0a2"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/integridad-publica-para-una-respuesta-y-recuperacion-efectivas-ante-el-covid-19", "c3d8f08f"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/investment-in-the-mena-region-in-the-time-of-covid-19", "da23e4c9"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/investment-promotion-agencies-in-the-time-of-covid-19", "50f79678"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/investment-screening-in-times-of-covid-19-and-beyond", "aa60af47"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/italian-regional-sme-policy-responses", "aa0eebbc"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/job-retention-schemes-during-the-covid-19-lockdown-and-beyond", "0853ba1d"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/keeping-the-internet-up-and-running-in-times-of-crisis", "4017c4c9"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/l-acces-aux-vaccins-anti-covid-19-dans-un-monde-en-crise-etat-des-lieux-et-strategies", "fe64d679"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/l-afrique-face-au-covid-19-implications-socio-economiques-regionales-et-priorites-politiques", "5b743bd8"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/l-impact-territorial-du-covid-19-gerer-la-crise-entre-niveaux-de-gouvernement", "2596466b"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/l-integrite-publique-au-service-d-une-reponse-et-d-un-relevement-efficaces-face-au-covid-19", "aaf16dd4"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/l-investissement-dans-la-region-mena-a-l-heure-du-covid-19", "03cbddc6"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/la-fonction-publique-face-a-la-pandemie-de-coronavirus-covid-19-premieres-actions-et-recommandations-initiales", "6f89770a"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/le-commerce-electronique-au-temps-de-la-pandemie-de-covid-19", "b0b1ce3e"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/le-conge-de-maladie-paye-pour-proteger-les-revenus-la-sante-et-les-emplois-pendant-la-crise-du-covid-19", "156ab874"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/le-covid-19-et-le-secteur-de-l-aviation-impact-et-mesures-adoptees-par-les-pouvoirs-publics", "8948a9b1"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/le-covid-19-et-le-secteur-du-commerce-de-detail-impact-et-mesures-de-politique-publique", "affc2e6b"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/le-dynamisme-des-entreprises-pendant-la-pandemie-de-covid-19-quelles-politiques-pour-une-reprise-inclusive", "105f1e14"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/learning-remotely-when-schools-close-how-well-are-students-and-schools-prepared-insights-from-pisa", "3bfda1f7"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/legislative-budget-oversight-of-emergency-responses-experiences-during-the-coronavirus-covid-19-pandemic", "ba4f2ab5"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/leitlinien-zu-den-verrechnungspreisfolgen-der-covid-19-pandemie", "752115f6"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/les-actions-engagees-dans-le-domaine-du-tourisme-face-au-coronavirus-covid-19", "86db4328"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/les-capacites-en-termes-de-teletravail-peuvent-entrainer-des-couts-de-confinement-differents-selon-les-territoires", "08920ecf"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/les-dispositifs-de-maintien-dans-l-emploi-pendant-la-periode-de-confinement-de-la-crise-du-covid-19-et-au-dela", "d315c5f1"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/les-mesures-adoptees-par-les-villes-face-au-covid-19", "aebdbf1c"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/les-possibilites-de-l-apprentissage-en-ligne-pour-les-adultes-premiers-enseignements-de-la-crise-du-covid-19", "0ef7c9bf"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/les-reponses-de-la-politique-de-la-concurrence-de-l-ocde-face-au-covid-19", "9348166d"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/les-reponses-de-politiques-fiscale-et-budgetaire-a-la-crise-du-coronavirus-accroitre-la-confiance-et-la-resilience", "32128119"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/leveraging-digital-trade-to-fight-the-consequences-of-covid-19", "f712f404"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/lidando-com-os-riscos-de-seguranca-digital-durante-a-crise-da-covid-19", "f4087e7c"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/lorsqu-un-virus-mondial-rencontre-des-realites-locales-coronavirus-covid-19-en-afrique-de-l-ouest", "16f49237"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/maintenir-l-acces-a-l-internet-en-temps-de-crise", "3cd99153"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/making-the-green-recovery-work-for-jobs-income-and-growth", "a505f3e7"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/managing-for-sustainable-results-in-development-co-operation-in-uncertain-times", "c94f0b59"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/managing-international-migration-under-covid-19", "6e914d57"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/manteniendo-el-internet-en-marchaen-tiempos-de-crisis", "e5528cf8"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/mehr-als-eindammung-antworten-der-oecd-gesundheitssysteme-auf-covid-19", "e446c943"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/mettre-la-relance-verte-au-service-de-l-emploi-des-revenus-et-de-la-croissance", "899c5467"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/mise-a-jour-des-orientations-sur-les-conventions-fiscales-et-impact-de-la-pandemie-de-covid-19", "4d797d39"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/mit-dem-homeoffice-potenzial-konnen-auch-die-lockdown-kosten-verschiedener-standorte-variieren", "d181196c"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/mobiliser-la-main-d-uvre-pendant-la-crise-du-covid-19-mesures-en-matiere-de-competences", "28032cdc"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/no-policy-maker-is-an-island-the-international-regulatory-co-operation-response-to-the-covid-19-crisis", "3011ccd0"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/o-combate-a-desinformacao-sobre-covid-19-em-plataformas-online", "7dc5c89d"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/oecd-competition-policy-responses-to-covid-19", "5c47af5a"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/oecd-initiative-for-safe-international-mobility-during-the-covid-19-pandemic-including-blueprint", "d0594162"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/oecd-investment-policy-responses-to-covid-19", "4be0254d"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/oecd-secretariat-analysis-of-tax-treaties-and-the-impact-of-the-covid-19-crisis", "947dcb01"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/one-year-of-sme-and-entrepreneurship-policy-responses-to-covid-19-lessons-learned-to-build-back-better", "9a230220"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/paid-sick-leave-to-protect-income-health-and-jobs-through-the-covid-19-crisis", "a9e1a154"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/parceria-social-nos-tempos-da-pandemia-covid-19", "cf20df55"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/peche-aquaculture-et-covid-19-enjeux-et-reponses-politiques", "f2c4b74d"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/policy-implications-of-coronavirus-crisis-for-rural-development", "6b9d189a"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/policy-measures-to-avoid-corruption-and-bribery-in-the-covid-19-response-and-recovery", "225abff3"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/policy-responses-to-covid-19-in-the-seed-sector", "1e9291db"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/politicas-de-reposta-das-cidades", "4a98f3a8"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/por-que-a-ciencia-aberta-e-fundamental-no-combate-a-covid-19", "ca4bdcf9"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/por-que-la-ciencia-abierta-es-esencial-para-combatir-el-covid-19", "f3b83813"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/pour-soutenir-la-lutte-contre-le-covid-19-une-meilleure-harmonisation-des-reglementations-relatives-aux-essais-cliniques-s-impose", "dda56a39"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/prestar-asesoramiento-cientifico-a-los-responsables-de-la-formulacion-de-politicas-durante-la-pandemia-de-covid-19", "181e448e"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/productivity-gains-from-teleworking-in-the-post-covid-19-era-how-can-public-policies-make-it-happen", "a5d52e99"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/proteccion-de-los-programas-de-beneficios-sociales-derivados-del-covid-19-contra-fraudes-y-errores", "6a535752"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/protecting-online-consumers-during-the-covid-19-crisis", "2ce7353c"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/providing-science-advice-to-policy-makers-during-covid-19", "4eec08c5"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/public-employment-services-in-the-frontline-for-employees-jobseekers-and-employers", "c986ff92"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/public-integrity-for-an-effective-covid-19-response-and-recovery", "a5c35d8c"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/public-procurement-and-infrastructure-governance-initial-policy-responses-to-the-coronavirus-covid-19-crisis", "c0ab0a96"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/public-servants-and-the-coronavirus-covid-19-pandemic-emerging-responses-and-initial-recommendations", "253b1277"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/qu-ont-fait-les-plateformes-pour-proteger-les-travailleurs-pendant-la-crise-du-coronavirus-covid-19", "9cc1e75d"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/rastreamento-e-monitoramento-da-covid-protecao-da-privacidade-e-dos-dados-pessoais-na-utilizacao-de-aplicativos-e-biometria", "78260de1"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/rastreo-y-seguimiento-del-covid-19-proteccion-de-la-privacidad-y-los-datos-en-el-uso-de-aplicaciones-y-biometria", "af3cc887"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/realizar-pruebas-para-la-deteccion-de-la-covid-19-una-forma-de-levantar-las-restricciones-de-confinamiento", "76e1c9d1"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/rebuilding-tourism-for-the-future-covid-19-policy-responses-and-recovery", "bced9859"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/rechtsetzungsqualitat-und-covid-19-risiken-bewaltigen-und-den-wiederaufbau-fordern", "a704d0ea"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/reconstruir-mejor-por-una-recuperacion-resiliente-y-sostenible-despues-del-covid-19", "8ccb61b8"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/reconstruire-en-mieux-pour-une-reprise-durable-et-resiliente-apres-le-covid-19", "583cf0b8"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/reconstruire-le-tourisme-de-demain-reponses-des-pouvoirs-publics-au-covid-19-et-reprise", "56639ffa"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/regulatory-policy-and-covid-19-behavioural-insights-for-fast-paced-decision-making", "7a521805"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/regulatory-quality-and-covid-19-managing-the-risks-and-supporting-the-recovery", "3f752e60"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/regulatory-quality-and-covid-19-the-use-of-regulatory-management-tools-in-a-time-of-crisis", "b876d5dc"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/removing-administrative-barriers-improving-regulatory-delivery", "6704c8a1"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/renforcer-la-premiere-ligne-comment-les-soins-primaires-aident-les-systemes-de-sante-a-s-adapter-a-la-pandemie-de-covid-19", "ae139cf5"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/reponse-a-la-crise-du-covid-19-dans-les-pays-de-la-region-mena", "082e24c2"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/reponses-de-l-administration-fiscale-au-covid-19-considerations-liees-a-la-continuite-de-l-activite", "ef1e8f04"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/reponses-de-l-administration-fiscale-au-covid-19-mesures-prises-pour-soutenir-les-contribuables", "69d26e77"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/reponses-de-l-administration-fiscale-face-au-covid-19-planifier-la-phase-de-reprise", "fe863859"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/responding-to-the-covid-19-and-pandemic-protection-gap-in-insurance", "35e74736"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/response-recovery-and-prevention-in-the-coronavirus-covid-19-pandemic-in-developing-countries-women-and-girls-on-the-frontlines", "23d645da"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/respostas-da-administracao-tributaria-a-covid-19-consideracoes-sobre-a-continuidade-dos-servicos", "7ffd3180"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/respuesta-de-las-administraciones-tributarias-al-covid-19-consideraciones-acerca-de-la-continuidad-de-actividades-y-servicios", "1faead46"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/respuestas-educativas-a-covid-19-adoptar-el-aprendizaje-digital-y-la-colaboracion-en-linea", "e6907480"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/respuestas-ocde-de-politica-de-competencia-ante-la-crisis-de-covid-19", "d99c6f1f"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/respuestas-politicas-de-las-ciudades-al-covid-19", "12646989"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/retirement-savings-in-the-time-of-covid-19", "b9740518"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/risiken-fur-den-unternehmenssektor-in-der-covid-19-krise-beurteilung-und-politikreaktionen", "5776b9e1"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/risks-that-matter-2020-the-long-reach-of-covid-19", "44932654"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/risques-lies-a-la-securite-numerique-pendant-la-crise-du-coronavirus-covid-19", "ba8e6d3a"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/safeguarding-covid-19-social-benefit-programmes-from-fraud-and-error", "4e21c80e"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/salud-ambiental-y-resiliencia-ante-las-pandemias", "3788e625"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/sante-environnementale-et-renforcement-de-la-resilience-face-aux-pandemies", "25111ac9"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/saude-ambiental-e-fortalecendo-a-resiliencia-a-pandemias", "54eb1a65"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/scaling-up-policies-that-connect-people-with-jobs-in-the-recovery-from-covid-19", "a91d2087"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/science-technologie-et-innovation-la-coordination-nationale-au-service-de-la-lutte-mondiale-contre-le-covid-19", "b18ecb4a"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/science-technology-and-innovation-how-co-ordination-at-home-can-help-the-global-fight-against-covid-19", "aa547c11"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/securing-the-recovery-ambition-and-resilience-for-the-well-being-of-children-in-the-post-covid-19-decade", "0f02237a"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/servicios-publicos-de-empleo-en-primera-linea-para-solicitantes-de-empleo-trabajadores-y-empleadores", "7a921e6c"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/servidores-publicos-e-a-pandemia-de-coronavirus-covid-19-respostas-emergentes-e-recomendacoes-iniciais", "9f2bd471"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/seven-lessons-learned-about-digital-security-during-the-covid-19-crisis", "e55a6b9a"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/shock-cultura-covid-19-e-settori-culturali-e-creativi", "e9ef83e6"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/siete-lecciones-aprendidas-sobre-seguridad-digital-durante-la-crisis-de-covid-19", "c8fa9059"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/skill-measures-to-mobilise-the-workforce-during-the-covid-19-crisis", "afd33a65"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/social-economy-and-the-covid-19-crisis-current-and-future-roles", "f904b89f"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/soutenir-l-emploi-et-les-entreprises-une-des-cles-de-la-reprise", "4cb4c30c"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/start-ups-in-the-time-of-covid-19-facing-the-challenges-seizing-the-opportunities", "87219267"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/stocktaking-report-on-immediate-public-procurement-and-infrastructure-responses-to-covid-19", "248d0646"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/strategic-foresight-for-the-covid-19-crisis-and-beyond-using-futures-thinking-to-design-better-public-policies", "c3448fa5"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/strengthening-health-systems-during-a-pandemic-the-role-of-development-finance", "f762bf1c"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/strengthening-online-learning-when-schools-are-closed-the-role-of-families-and-teachers-in-supporting-students-during-the-covid-19-crisis", "c4ecba6c"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/strengthening-the-frontline-how-primary-health-care-helps-health-systems-adapt-during-the-covid-19-pandemic", "9a5ae6da"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/suivi-et-tracage-du-covid-19-proteger-la-vie-privee-et-les-donnees-lors-de-l-utilisation-d-applications-et-de-la-biometrie", "40a928d1"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/supporting-businesses-in-financial-distress-to-avoid-insolvency-during-the-covid-19-crisis", "b4154a8b"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/supporting-jobs-and-companies-a-bridge-to-the-recovery-phase", "08962553"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/supporting-livelihoods-during-the-covid-19-crisis-closing-the-gaps-in-safety-nets", "17cbb92d"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/supporting-people-and-companies-to-deal-with-the-covid-19-virus-options-for-an-immediate-employment-and-social-policy-response", "d33dffe6"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/supporting-young-people-s-mental-health-through-the-covid-19-crisis", "84e143e5"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/tackling-the-mental-health-impact-of-the-covid-19-crisis-an-integrated-whole-of-society-response", "0ccafa0b"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/tax-administration-digital-resilience-in-the-covid-19-environment", "2f3cf2fb"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/tax-administration-privacy-disclosure-and-fraud-risks-related-to-covid-19", "950d8ed2"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/tax-administration-responses-to-covid-19-assisting-wider-government", "0dc51664"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/tax-administration-responses-to-covid-19-business-continuity-considerations", "953338dc"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/tax-administration-responses-to-covid-19-measures-taken-to-support-taxpayers", "adc84188"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/tax-administration-responses-to-covid-19-recovery-period-planning", "0ab5481d"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/tax-and-fiscal-policy-in-response-to-the-coronavirus-crisis-strengthening-confidence-and-resilience", "60f640a8"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/teaching-and-learning-in-vet-providing-effective-practical-training-in-school-based-settings", "64f5f843"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/testando-para-a-covid-19-uma-maneira-de-flexibilizar-as-restricoes-do-confinamento", "d8bbac2f"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/testing-for-covid-19-a-way-to-lift-confinement-restrictions", "89756248"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/testing-for-covid-19-how-to-best-use-the-various-tests", "c76df201"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/the-covid-19-crisis-a-catalyst-for-government-transformation", "1d0c0788"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/the-covid-19-crisis-and-state-ownership-in-the-economy-issues-and-policy-considerations", "ce417c46"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/the-effect-of-covid-19-on-alcohol-consumption-and-policy-responses-to-prevent-harmful-alcohol-consumption", "53890024"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/the-face-mask-global-value-chain-in-the-covid-19-outbreak-evidence-and-policy-lessons", "a4df866d"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/the-impact-of-coronavirus-covid-19-and-the-global-oil-price-shock-on-the-fiscal-position-of-oil-exporting-developing-countries", "8bafbd95"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/the-impact-of-coronavirus-covid-19-on-forcibly-displaced-persons-in-developing-countries", "88ad26de"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/the-impact-of-covid-19-on-agricultural-markets-and-ghg-emissions", "57e5eb53"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/the-impact-of-covid-19-on-student-equity-and-inclusion-supporting-vulnerable-students-during-school-closures-and-school-re-openings", "d593b5c8"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/the-impact-of-the-coronavirus-covid-19-crisis-on-development-finance", "9de00b3b"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/the-impacts-of-covid-19-on-the-space-industry", "e727e36f"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/the-long-term-environmental-implications-of-covid-19", "4b7a9937"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/the-oecd-green-recovery-database", "47ae0f0d"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/the-potential-of-online-learning-for-adults-early-lessons-from-the-covid-19-crisis", "ee040002"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/the-role-of-online-platforms-in-weathering-the-covid-19-shock", "2a3b8434"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/the-role-of-transparency-in-avoiding-a-covid-19-induced-food-crisis", "d6a37aeb"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/the-territorial-impact-of-covid-19-managing-the-crisis-across-levels-of-government", "d3e314e1"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/the-territorial-impact-of-covid-19-managing-the-crisis-and-recovery-across-levels-of-government", "a2c6abaf"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/tourism-policy-responses-to-the-coronavirus-covid-19", "6466aa20"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/towards-gender-inclusive-recovery", "ab597807"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/tracking-and-tracing-covid-protecting-privacy-and-data-while-using-apps-and-biometrics", "8f394636"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/trade-facilitation-and-the-covid-19-pandemic", "094306d2"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/trade-finance-in-the-covid-era-current-and-future-challenges", "79daca94"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/trade-finance-in-times-of-crisis-responses-from-export-credit-agencies", "946a21db"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/trade-interdependencies-in-covid-19-goods", "79aaa1d6"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/transparence-communication-et-confiance-le-role-de-la-communication-publique-pour-combattre-la-vague-de-desinformation-concernant-le-nouveau-coronavirus", "1d566531"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/transparency-communication-and-trust-the-role-of-public-communication-in-responding-to-the-wave-of-disinformation-about-the-new-coronavirus", "bef7ad6e"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/treatments-and-a-vaccine-for-covid-19-the-need-for-coordinating-policies-on-r-d-manufacturing-and-access", "6e7669a9"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/updated-guidance-on-tax-treaties-and-the-impact-of-the-covid-19-pandemic", "df42be07"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/usando-a-inteligencia-artificial-para-ajudar-no-combate-a-covid-19", "a569dd72"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/usar-el-comercio-para-combatir-la-covid-19-produccion-y-distribucion-de-vacunas", "59660b60"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/using-artificial-intelligence-to-help-combat-covid-19", "ae4c5c21"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/using-trade-to-fight-covid-19-manufacturing-and-distributing-vaccines", "dc0d37fc"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/uso-de-la-inteligencia-artificial-para-luchar-contra-la-pandemia-del-covid-19", "8c381c4e"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/utiliser-l-intelligence-artificielle-au-service-de-la-lutte-contre-le-covid-19", "0ef5d4f9"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/vet-in-a-time-of-crisis-building-foundations-for-resilient-vocational-education-and-training-systems", "efff194c"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/walking-the-tightrope-avoiding-a-lockdown-while-containing-the-virus", "1b912d4a"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/what-have-countries-done-to-support-young-people-in-the-covid-19-crisis", "ac9f056c"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/what-have-platforms-done-to-protect-workers-during-the-coronavirus-covid-19-crisis", "9d1c7aa2"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/what-is-the-impact-of-the-covid-19-pandemic-on-immigrants-and-their-children", "e7cbb7de"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/when-a-global-virus-meets-local-realities-coronavirus-covid-19-in-west-africa", "8af7f692"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/when-the-going-gets-tough-the-tough-get-going-how-economic-regulators-bolster-the-resilience-of-network-industries-in-response-to-the-covid-19-crisis", "cd8915b1"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/why-open-science-is-critical-to-combatting-covid-19", "cd6ab2f9"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/women-at-the-core-of-the-fight-against-covid-19-crisis", "553a8269"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/workforce-and-safety-in-long-term-care-during-the-covid-19-pandemic", "43fc5d50"),
            new KeyValuePair<string, string>("coronavirus/policy-responses/youth-and-covid-19-response-recovery-and-resilience", "c40e61c6")
        };

        /// <summary>
        /// used to filter raw data from GA to get only urls related to PR
        /// </summary>
        /// <param name="url">url from GA raw data</param>
        /// <returns>boolean result</returns>
        public static bool IsPolicyResponseUrl(string url)
        {
            url = url.ToLower();
            return IsPolicyResponseReadUrl(url) || IsPolicyResponseOecdUrl(url);
        }

        /// <summary>
        /// apply specific Read url filters
        /// </summary>
        /// <param name="url">url from GA raw data</param>
        /// <returns>boolean result</returns>
        private static bool IsPolicyResponseReadUrl(string url)
        {
            return
                //readURL part is present
                url.Contains(_readURL)
                // then part after "..?ref=" must have at least 21 chars
                && url.After(_readURL).Length >= 21;
        }

        /// <summary>
        /// apply specific Oecd url filters
        /// </summary>
        /// <param name="url">url from GA raw data</param>
        /// <returns>boolean result</returns>
        private static bool IsPolicyResponseOecdUrl(string url)
        {
            return
                // oecd covid hub url is present : "oecd.org/coronavirus/policy-responses/"
                (url.Contains(_oecdURL)
                // then match the oecdPattern
                && Regex.Matches(url + "/", _oecdPattern).Count > 0)
                // Or last chance case for url without id but are still PR url
                || GetIdFromOecdURLPart(url).Length > 0;
        }


        /// <summary>
        /// get the policy reponse id
        /// </summary>
        /// <param name="url">url of GA or Kappa report</param>
        /// <returns>id of the PR (could return many strings, but only one real id, for one url but nervermind the wrong ones will discarded further)</returns>
        public static string[] GetIdFromUrl(string url)
        {
            //remove all control and other non-printable characters
            url = Regex.Replace(url, @"\p{C}+", string.Empty).ToLower();
            if (IsPolicyResponseReadUrl(url))
            {
                return new string[] { GetIdFromReadUrl(url) };
            }
            if (url.Contains(_oecdURL))
            {
                return GetIdFromOecdUrl(url);
            }
            return new string[] { url.Trim() };
        }

        /// <summary>
        /// get the policy reponse id from Read url
        /// </summary>
        /// <param name="url">url from Read url</param>
        /// <returns>id of the PR</returns>
        private static string GetIdFromReadUrl(string url)
        {
            // get the part after "...?ref="
            var urlcleaned = url.After(_readURL);
            // remove url params and other intrusive separators/words found in GA raw data
            urlcleaned = urlcleaned.Split(new string[] { "&", "?", "\\", "title", "country", "hyperlink", "and" }, StringSplitOptions.None)[0];
            return urlcleaned.Trim();
        }

        /// <summary>
        /// get the policy reponse id from Oecd url
        /// </summary>
        /// <param name="url">url of GA or Kappa report</param>
        /// <returns>id of the PR (could return many as the regexp may match many id patterns (no way to be more selective), but nervermind the wrong ones will discarded further)</returns>
        private static string[] GetIdFromOecdUrl(string url)
        {
            var matches = Regex.Matches(url + "/", _oecdPattern)
                .Cast<Match>()
                .Select(m => m.Value.Trim())
                .ToArray();
            // return result from regexp if not get the id from static url list
            return matches.Length > 0 ? matches : new string[] { GetIdFromOecdURLPart(url) };
        }

        /// <summary>
        /// get the id of the PR based on a part of the url 
        /// this function is used in the last position order
        /// </summary>
        /// <param name="url">Part of the url</param>
        /// <returns>id of the PR</returns>
        private static string GetIdFromOecdURLPart(string url)
        {
            var PRid = string.Empty;
            foreach (KeyValuePair<string, string> oecdUrl in _listStaticOecdUrls)
            {
                if (url.Contains(oecdUrl.Key))
                {
                    PRid = oecdUrl.Value;
                    break;
                }
            }
            return PRid;
        }

        /// <summary>
        /// Get a Datatable from the XML document of the PR repor
        /// </summary>
        /// <param name="document">XML document of the PR report</param>
        /// <returns>A Datatable ready to be merged with GA data (extra REF column, Dirs and themes splitted)</returns>
        public static DataTable GenerateKappaDataTable(XDocument document)
        {
            var dt = new DataTable();

            /* ************************* */
            /*        columns            */
            /* ************************* */
            // columns order : REF, all the others (minus the original grouped theme column), the 3 splited themes
            var columns = new List<DataColumn>();
            // themes field is the concatenation of all themes, they will be splited into 3 cells
            var cellsthemesNames = new List<string> { "Theme 1", "Theme 2", "Theme 3 (and more)" };
            // same for directorates
            var cellsDirectoratesNames = new List<string> { "Dir 1", "Dir 2", "Dir 3 (and more)" };

            XElement root = document.Root;
            XElement firstRow = root.Descendants("row").First();

            List<XElement> xCols;
            //get header cells labels
            xCols = firstRow.Descendants("cell").ToList();
            //add 'REF' column to store the computed id of the policy responses
            columns.Add(new DataColumn("REF"));
            // all the other columns
            columns.AddRange(xCols.Select(c => new DataColumn(c.Value)));
            // add themes header labels
            columns.AddRange(cellsthemesNames.Select(c => new DataColumn(c)));
            // add directorates header labels
            columns.AddRange(cellsDirectoratesNames.Select(c => new DataColumn(c)));
            // insert colums in the datatable
            dt.Columns.AddRange(columns.ToArray());
            // set the primary key to be able to join with other GA datatable
            dt.PrimaryKey = new DataColumn[] { dt.Columns["REF"] };

            /* ************************* */
            /*          rows             */
            /* ************************* */
            List<XElement> xrows = root.Descendants("row").Skip(1).ToList();
            //int index = result.Columns["Themes"].Ordinal;
            // columns label that contained the id 
            var cellsURLNames = new List<string> { "oecd.org url", "mediahub link" };
            foreach (XElement xrow in xrows)
            {
                List<XElement> xCells;
                xCells = xrow.Descendants("cell").ToList();
                var xUrl = xCells.FirstOrDefault(c => cellsURLNames.Contains(c.Attribute("name").Value) && c.Value.Length > 0)?.Value;
                // some policy responses has only a blog url (no oecd.org or read)
                if (string.IsNullOrEmpty(xUrl) || !IsPolicyResponseUrl(xUrl))
                    continue;
                var refValue = GetIdFromUrl(xUrl)[0];

                // split grouped themes to get indiviual theme (4th and further theme will be kept with the 3rd one))
                var themes = new List<string>();
                themes = Enumerable.Repeat(string.Empty, 3).ToList();
                themes = xCells.FirstOrDefault(c => c.Attribute("name").Value == "themes").Value
                    .Split('|', 3, StringSplitOptions.TrimEntries).ToList();

                var directorates = new List<string>();
                directorates = Enumerable.Repeat(string.Empty, 3).ToList();
                directorates = xCells.FirstOrDefault(c => c.Attribute("name").Value == "directorates").Value
                    .Split('|', 3, StringSplitOptions.TrimEntries).ToList();

                var r = dt.NewRow();
                var data = new List<string>
                {
                    refValue
                };
                // add all values
                data.AddRange(xCells.Select(c => c.Value)); //.Where(c => c.Attribute("name").Value != "themes")
                // add the 3 splited themes
                for (var i = 0; i < 3; i++)
                {
                    data.Add(themes.ElementAtOrDefault(i) ?? string.Empty);
                }
                for (var i = 0; i < 3; i++)
                {
                    data.Add(directorates.ElementAtOrDefault(i)?.ToUpper() ?? string.Empty);
                }
                for (var i = 0; i < data.Count; i++)
                {
                    r[i] = data[i];
                }

                dt.Rows.Add(r);
            }

            return dt;
        }

        /// <summary>
        /// Get raw data from GoogleAnalytics API and returns a datatable filtered : containing only PR with their ids
        /// </summary>
        /// <param name="report">AnalyticReport object generated by GoogleAnalyticsAPI class containing raw data basically filtered</param>
        /// <returns>A datable with an extra 'REF' column containing the PR id</returns>
        public static DataTable GenerateCleanedDataTable(AnalyticReport report)
        {
            var dt = new DataTable();

            var columns = new List<DataColumn>();
            var dimensionColumns = report.ColumnHeader.Dimensions;
            columns.AddRange(dimensionColumns.Select(c => new DataColumn(c)));

            var metricColumns = report.ColumnHeader.MetricHeader.MetricHeaderEntries;
            columns.AddRange(metricColumns.Select(m => new DataColumn(m.Name, Type.GetType("System.Int32"))));

            dt.Columns.Add(new DataColumn("REF"));
            dt.Columns.AddRange(columns.ToArray());

            int index = dt.Columns["ga:pagePath"].Ordinal - 1;

            var rows = report.Rows;
            foreach (var row in rows)
            {
                var dimensions = row.Dimensions;
                var xUrl = dimensions[index];

                if (string.IsNullOrEmpty(xUrl) || !IsPolicyResponseUrl(xUrl))
                    continue;

                var metrics = row.Metrics.First().Values;

                // the regexp could return many values (no matter, there is only one good value for the id, the wrong ones will be discarded after)
                string[] refValues = GetIdFromUrl(xUrl);
                foreach (var refValue in refValues)
                {
                    var r = dt.NewRow();
                    var data = new List<string>
                    {
                        refValue
                    };
                    data.AddRange(dimensions);
                    data.AddRange(metrics);
                    for (var i = 0; i < data.Count; i++)
                    {
                        r[i] = data[i];
                    }
                    dt.Rows.Add(r);
                }
            }

            return dt;
        }

        /// <summary>
        /// Takes a datatable and retuns a new datable grouped by 'REF' column and sum "views" column alias 
        /// </summary>
        /// <param name="dt">A datatable with REF and "views" column alias</param>
        /// <returns>A datatable grouped by 'REF' values and sum "views" column alias</returns>
        public static DataTable GroupDataTableByRef(DataTable dt)
        {
            var dtGroupedBy = new DataTable();
            dtGroupedBy.Columns.Add("REF", typeof(string));
            dtGroupedBy.Columns.Add("pageviews", typeof(int));
            dtGroupedBy = dt.AsEnumerable()
                .GroupBy(row => row.Field<string>("REF"))
                .Select(g =>
                {
                    var row = dtGroupedBy.NewRow();
                    row.SetField("REF", g.Key);
                    row.SetField("pageviews", g.Sum(x => x.Field<int>("views")));
                    return row;
                })
                .CopyToDataTable();
            dtGroupedBy.PrimaryKey = new DataColumn[] { dtGroupedBy.Columns["REF"] };
            return dtGroupedBy;
        }

        /// <summary>
        /// Takes 2 datatables (GA and Kappa) and merges them into one datable based on their commun REF data
        /// </summary>
        /// <param name="GAData">A datatable containing GA data with REF column</param>
        /// <param name="KappaData">>A datatable containing Kappa data with REF column<</param>
        /// <returns>A merged datatable based on REF column</returns>
        public static DataTable MergeKappaAndGADataTables(DataTable GAData, DataTable KappaData)
        {
            var dtMerged = new DataTable();
            dtMerged = GAData.JoinDataTables(
                    KappaData,
                    (row1, row2) => row1.Field<string>("REF") == row2.Field<string>("REF")
                );
            dtMerged.SetColumnsOrder(new string[] { "pageviews", "Work Title", "Title", "Subtitle", "Language", "Medium", "Theme 1", "Theme 2", "Theme 3 (and more)", "Dir 1", "Dir 2", "Dir 3 (and more)", "Web Topics", "Keywords", "Availability", "Date of Publication", "iLibrary Access Type", "Manifestation ID", "Expression ID", "Work ID", "REF", "Oecd.Org Url", "MediaHub Link" });
            DataView dv = dtMerged.DefaultView;
            dv.Sort = "Work Title, Language, Medium";
            DataTable dtSorted = dv.ToTable();
            return dtSorted;
        }
    }
}
