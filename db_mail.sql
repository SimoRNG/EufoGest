-- --------------------------------------------------------
-- Host:                         127.0.0.1
-- Versione server:              12.0.2-MariaDB - mariadb.org binary distribution
-- S.O. server:                  Win64
-- HeidiSQL Versione:            12.11.0.7065
-- --------------------------------------------------------

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET NAMES utf8 */;
/*!50503 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;


-- Dump della struttura del database db_gestione_main
CREATE DATABASE IF NOT EXISTS `db_gestione_main` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_uca1400_ai_ci */;
USE `db_gestione_main`;

-- Dump della struttura di tabella db_gestione_main.tab_allegati_compiti
CREATE TABLE IF NOT EXISTS `tab_allegati_compiti` (
  `id_compito` int(10) unsigned NOT NULL,
  `allegato_hash` varchar(255) NOT NULL DEFAULT ''
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Dump dei dati della tabella db_gestione_main.tab_allegati_compiti: ~0 rows (circa)

-- Dump della struttura di tabella db_gestione_main.tab_assegnazione_compiti
CREATE TABLE IF NOT EXISTS `tab_assegnazione_compiti` (
  `id_compito` int(10) unsigned NOT NULL,
  `id_cliente` int(10) unsigned NOT NULL,
  UNIQUE KEY `id_compito_id_cliente` (`id_compito`,`id_cliente`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Dump dei dati della tabella db_gestione_main.tab_assegnazione_compiti: ~0 rows (circa)

-- Dump della struttura di tabella db_gestione_main.tab_aule
CREATE TABLE IF NOT EXISTS `tab_aule` (
  `codice` varchar(5) NOT NULL DEFAULT '',
  `descrizione` varchar(50) NOT NULL DEFAULT '',
  `agibile` bit(1) NOT NULL DEFAULT b'1',
  PRIMARY KEY (`codice`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Dump dei dati della tabella db_gestione_main.tab_aule: ~3 rows (circa)
INSERT INTO `tab_aule` (`codice`, `descrizione`, `agibile`) VALUES
	('1', 'AULA 1', b'1'),
	('2', 'AULA 2', b'1'),
	('3', 'AULA 3', b'1');

-- Dump della struttura di tabella db_gestione_main.tab_clienti
CREATE TABLE IF NOT EXISTS `tab_clienti` (
  `id` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `nome` varchar(50) NOT NULL,
  `cognome` varchar(50) NOT NULL,
  `data_nascita` datetime DEFAULT NULL,
  `numero_telefono` varchar(15) DEFAULT NULL,
  `email` varchar(100) DEFAULT NULL,
  `attivo` bit(1) NOT NULL DEFAULT b'0',
  `dataora_creazione` datetime DEFAULT NULL,
  `dataora_ultima_modifica` datetime DEFAULT NULL,
  `data_iscrizione` datetime DEFAULT NULL,
  PRIMARY KEY (`id`) USING BTREE,
  KEY `IX_attivo` (`attivo`)
) ENGINE=InnoDB AUTO_INCREMENT=17 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Dump dei dati della tabella db_gestione_main.tab_clienti: ~0 rows (circa)

-- Dump della struttura di tabella db_gestione_main.tab_compiti
CREATE TABLE IF NOT EXISTS `tab_compiti` (
  `id` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `consegna` text DEFAULT '',
  `id_assegnatore` int(10) unsigned DEFAULT NULL,
  `rif_lezione_anno` int(11) DEFAULT NULL,
  `rif_lezione_numero` bigint(20) DEFAULT NULL,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Dump dei dati della tabella db_gestione_main.tab_compiti: ~0 rows (circa)

-- Dump della struttura di tabella db_gestione_main.tab_files_unici
CREATE TABLE IF NOT EXISTS `tab_files_unici` (
  `id` int(11) NOT NULL AUTO_INCREMENT,
  `hash_file` varchar(64) NOT NULL,
  `nome_file` varchar(255) NOT NULL,
  `mime_type` varchar(100) NOT NULL,
  `percorso_file` varchar(512) NOT NULL,
  PRIMARY KEY (`id`) USING BTREE,
  UNIQUE KEY `HashFile` (`hash_file`) USING BTREE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Dump dei dati della tabella db_gestione_main.tab_files_unici: ~0 rows (circa)

-- Dump della struttura di tabella db_gestione_main.tab_funzionalita_abilitate
CREATE TABLE IF NOT EXISTS `tab_funzionalita_abilitate` (
  `username_utente` varchar(50) NOT NULL DEFAULT '',
  `codice_funzionalita` varchar(5) NOT NULL DEFAULT '',
  `abilitata` bit(1) NOT NULL DEFAULT b'1'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Dump dei dati della tabella db_gestione_main.tab_funzionalita_abilitate: ~0 rows (circa)

-- Dump della struttura di tabella db_gestione_main.tab_lezioni
CREATE TABLE IF NOT EXISTS `tab_lezioni` (
  `numero` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `anno` int(10) unsigned NOT NULL DEFAULT 0,
  `dataora_inizio` datetime NOT NULL DEFAULT '0000-00-00 00:00:00',
  `dataora_fine` datetime NOT NULL DEFAULT '0000-00-00 00:00:00',
  `utente_inserimento` varchar(50) DEFAULT '',
  `utente_ultima_modifica` varchar(50) DEFAULT '',
  `note` varchar(100) DEFAULT '',
  `stato` enum('CANCELLATA','PREVISIONALE','PROVVISORIA','CONFERMATA','IN CORSO','CONCLUSA') DEFAULT NULL,
  `lingua` varchar(5) NOT NULL,
  `cambio_stato_automatico` bit(1) DEFAULT b'0',
  `aula` varchar(5) DEFAULT NULL,
  `rowversion` timestamp NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  PRIMARY KEY (`numero`,`anno`) USING BTREE
) ENGINE=InnoDB AUTO_INCREMENT=10 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Dump dei dati della tabella db_gestione_main.tab_lezioni: ~0 rows (circa)

-- Dump della struttura di tabella db_gestione_main.tab_lezioni_impartite
CREATE TABLE IF NOT EXISTS `tab_lezioni_impartite` (
  `id_impartizione` int(11) NOT NULL AUTO_INCREMENT,
  `id_professore` int(10) unsigned NOT NULL,
  `numero_lezione` bigint(20) unsigned NOT NULL,
  `anno_lezione` int(10) unsigned NOT NULL,
  `firma_professore` binary(50) DEFAULT NULL,
  `paga_totale` double NOT NULL DEFAULT 0,
  `impartizione_avvenuta` bit(1) NOT NULL DEFAULT b'0',
  PRIMARY KEY (`id_impartizione`)
) ENGINE=InnoDB AUTO_INCREMENT=9 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Dump dei dati della tabella db_gestione_main.tab_lezioni_impartite: ~0 rows (circa)

-- Dump della struttura di tabella db_gestione_main.tab_lingua
CREATE TABLE IF NOT EXISTS `tab_lingua` (
  `codice` varchar(5) NOT NULL,
  `descrizione` varchar(20) DEFAULT NULL,
  `colore_hex` varchar(10) DEFAULT NULL,
  PRIMARY KEY (`codice`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Dump dei dati della tabella db_gestione_main.tab_lingua: ~11 rows (circa)
INSERT INTO `tab_lingua` (`codice`, `descrizione`, `colore_hex`) VALUES
	('ara', 'Arabo', '#449c69'),
	('chi', 'Cinese', '#faac6b'),
	('eng', 'Inglese', '#c66bfa'),
	('fra', 'Francese', '#5f5ffa'),
	('ger', 'Tedesco', '#bfa545'),
	('ita', 'Italiano', '#8bfca3'),
	('jpn', 'Giapponese', '#fc6b5b'),
	('kor', 'Coreano', '#ffffff'),
	('rus', 'Russo', '#92c4fc'),
	('spa', 'Spagnolo', '#fcd703'),
	('tai', 'Thailandese', '#ff73f3');

-- Dump della struttura di tabella db_gestione_main.tab_lingue_insegnate_professori
CREATE TABLE IF NOT EXISTS `tab_lingue_insegnate_professori` (
  `id_professore` int(10) unsigned NOT NULL,
  `codice_lingua` varchar(5) NOT NULL,
  UNIQUE KEY `id_professore_codice_lingua` (`id_professore`,`codice_lingua`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Dump dei dati della tabella db_gestione_main.tab_lingue_insegnate_professori: ~0 rows (circa)

-- Dump della struttura di tabella db_gestione_main.tab_lingue_studiate_clienti
CREATE TABLE IF NOT EXISTS `tab_lingue_studiate_clienti` (
  `id_cliente` int(10) unsigned NOT NULL,
  `codice_lingua` varchar(5) NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Dump dei dati della tabella db_gestione_main.tab_lingue_studiate_clienti: ~0 rows (circa)

-- Dump della struttura di tabella db_gestione_main.tab_log
CREATE TABLE IF NOT EXISTS `tab_log` (
  `id` bigint(20) unsigned NOT NULL DEFAULT 0,
  `dataora_log` datetime NOT NULL DEFAULT current_timestamp(),
  `messaggio` text NOT NULL DEFAULT '',
  `utente_coinvolto` int(11) DEFAULT NULL,
  `criticita` enum('DEBUG','INFORMAZIONE','AVVISO','ERRORE','CRITICO') NOT NULL DEFAULT 'INFORMAZIONE',
  PRIMARY KEY (`id`),
  KEY `idx_utente_coinvolto` (`utente_coinvolto`),
  KEY `idx_dataora_log` (`dataora_log`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Dump dei dati della tabella db_gestione_main.tab_log: ~0 rows (circa)

-- Dump della struttura di tabella db_gestione_main.tab_pacchetti_lezioni_usati
CREATE TABLE IF NOT EXISTS `tab_pacchetti_lezioni_usati` (
  `id_utilizzo` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `id_pacchetto` int(11) unsigned NOT NULL,
  `id_partecipazione` bigint(20) unsigned NOT NULL,
  `minuti_usati` int(10) unsigned NOT NULL,
  PRIMARY KEY (`id_utilizzo`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci COMMENT='Fa riferimento solo a tab_Partecipanti_Lezione e tab_Pacchetti_Ore.\r\nUtilizzata perché per una lezione posso usare più pacchetti. Es.: 2 ore di lezione, una pagata da un pacchetto ed una da un altro ';

-- Dump dei dati della tabella db_gestione_main.tab_pacchetti_lezioni_usati: ~0 rows (circa)

-- Dump della struttura di tabella db_gestione_main.tab_pacchetti_ore
CREATE TABLE IF NOT EXISTS `tab_pacchetti_ore` (
  `id` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `id_sottoscrittore` int(10) unsigned NOT NULL,
  `minuti_pacchetto` int(10) unsigned NOT NULL DEFAULT 0,
  `valido_dal` datetime DEFAULT NULL,
  `valido_al` datetime DEFAULT NULL,
  `attivo` bit(1) NOT NULL DEFAULT b'1',
  `costo` float unsigned NOT NULL DEFAULT 0,
  `importo_pagato` float unsigned NOT NULL DEFAULT 0,
  `dataora_sottoscrizione` datetime DEFAULT NULL,
  `note` text NOT NULL DEFAULT '',
  `minuti_rimasti` int(10) unsigned NOT NULL DEFAULT 0,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Dump dei dati della tabella db_gestione_main.tab_pacchetti_ore: ~0 rows (circa)

-- Dump della struttura di tabella db_gestione_main.tab_partecipanti_lezioni
CREATE TABLE IF NOT EXISTS `tab_partecipanti_lezioni` (
  `id_partecipazione` bigint(20) NOT NULL AUTO_INCREMENT,
  `id_cliente` int(10) unsigned NOT NULL,
  `numero_lezione` bigint(20) unsigned NOT NULL,
  `anno_lezione` int(10) unsigned NOT NULL,
  `costo_lezione` float DEFAULT NULL,
  `firma_cliente` varbinary(50) DEFAULT NULL,
  `partecipazione_avvenuta` bit(1) NOT NULL DEFAULT b'0',
  `minuti_scalati` bit(1) NOT NULL DEFAULT b'0',
  PRIMARY KEY (`id_partecipazione`)
) ENGINE=InnoDB AUTO_INCREMENT=8 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Dump dei dati della tabella db_gestione_main.tab_partecipanti_lezioni: ~0 rows (circa)

-- Dump della struttura di tabella db_gestione_main.tab_professori
CREATE TABLE IF NOT EXISTS `tab_professori` (
  `id` int(10) unsigned NOT NULL AUTO_INCREMENT,
  `nome` varchar(50) NOT NULL,
  `cognome` varchar(50) NOT NULL,
  `data_nascita` datetime DEFAULT NULL,
  `numero_telefono` varchar(15) DEFAULT NULL,
  `email` varchar(100) DEFAULT NULL,
  `attivo` bit(1) NOT NULL DEFAULT b'1',
  `dataora_creazione` datetime DEFAULT NULL,
  `dataora_ultima_modifica` datetime DEFAULT NULL,
  `inizio_contratto` datetime DEFAULT NULL,
  `fine_contratto` datetime DEFAULT NULL,
  `note` varchar(100) DEFAULT '',
  `paga_oraria` double NOT NULL DEFAULT 0,
  PRIMARY KEY (`id`) USING BTREE,
  KEY `IX_attivo` (`attivo`)
) ENGINE=InnoDB AUTO_INCREMENT=17 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Dump dei dati della tabella db_gestione_main.tab_professori: ~0 rows (circa)

-- Dump della struttura di tabella db_gestione_main.tab_refresh_token
CREATE TABLE IF NOT EXISTS `tab_refresh_token` (
  `id` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `username` varchar(50) NOT NULL,
  `token` varchar(255) NOT NULL,
  `scadenza` datetime NOT NULL,
  `revocato` bit(1) NOT NULL DEFAULT b'0',
  `dataora_creazione` datetime DEFAULT current_timestamp(),
  PRIMARY KEY (`id`),
  KEY `idx_token` (`token`),
  KEY `idx_username` (`username`)
) ENGINE=InnoDB AUTO_INCREMENT=80 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Dump dei dati della tabella db_gestione_main.tab_refresh_token: ~0 rows (circa)

-- Dump della struttura di tabella db_gestione_main.tab_ruoli_sistema
CREATE TABLE IF NOT EXISTS `tab_ruoli_sistema` (
  `id` tinyint(3) unsigned NOT NULL,
  `descrizione` varchar(25) NOT NULL DEFAULT '',
  `livello_accesso` tinyint(4) NOT NULL DEFAULT 0,
  `amministratore` bit(1) NOT NULL DEFAULT b'0',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Dump dei dati della tabella db_gestione_main.tab_ruoli_sistema: ~3 rows (circa)
INSERT INTO `tab_ruoli_sistema` (`id`, `descrizione`, `livello_accesso`, `amministratore`) VALUES
	(1, 'UTENTE', 1, b'0'),
	(2, 'OPERATORE', 10, b'0'),
	(3, 'AMMINISTRATORE', 20, b'1');

-- Dump della struttura di tabella db_gestione_main.tab_scuola
CREATE TABLE IF NOT EXISTS `tab_scuola` (
  `ragione_sociale` varchar(255) NOT NULL DEFAULT '',
  `nome_abbreviato` varchar(255) NOT NULL DEFAULT '',
  `chiave_licenza` varchar(255) DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci COMMENT='Info generali';

-- Dump dei dati della tabella db_gestione_main.tab_scuola: ~0 rows (circa)

-- Dump della struttura di tabella db_gestione_main.tab_utenti
CREATE TABLE IF NOT EXISTS `tab_utenti` (
  `username` varchar(50) NOT NULL,
  `password_hash` varchar(255) NOT NULL,
  `id_anagrafica` int(11) unsigned DEFAULT NULL,
  `tipo_anagrafica` varchar(1) DEFAULT NULL,
  `id_ruolo` tinyint(4) unsigned NOT NULL,
  `salt` varchar(64) NOT NULL,
  `ultimo_accesso` datetime DEFAULT NULL,
  `attivo` bit(1) NOT NULL DEFAULT b'1',
  `tentativi_login_falliti` tinyint(3) unsigned NOT NULL DEFAULT 0,
  `bloccato_fino_a` datetime DEFAULT NULL,
  PRIMARY KEY (`username`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_uca1400_ai_ci;

-- Dump dei dati della tabella db_gestione_main.tab_utenti: ~2 rows (circa)
INSERT INTO `tab_utenti` (`username`, `password_hash`, `id_anagrafica`, `tipo_anagrafica`, `id_ruolo`, `salt`, `ultimo_accesso`, `attivo`, `tentativi_login_falliti`, `bloccato_fino_a`) VALUES
	('ADMIN', 'nT4a9VOsfJ3wnzNf0VGW7kN8WHbmd6+Kjnf9M6BaffA=', NULL, NULL, 3, 'IGuB1XCE/AIguUzSMIeHkw==', '2026-10-29 01:18:08', b'1', 0, NULL),
	('USER', 'nT4a9VOsfJ3wnzNf0VGW7kN8WHbmd6+Kjnf9M6BaffA=', NULL, NULL, 1, 'IGuB1XCE/AIguUzSMIeHkw==', '2026-10-29 01:18:34', b'1', 0, NULL);

/*!40103 SET TIME_ZONE=IFNULL(@OLD_TIME_ZONE, 'system') */;
/*!40101 SET SQL_MODE=IFNULL(@OLD_SQL_MODE, '') */;
/*!40014 SET FOREIGN_KEY_CHECKS=IFNULL(@OLD_FOREIGN_KEY_CHECKS, 1) */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40111 SET SQL_NOTES=IFNULL(@OLD_SQL_NOTES, 1) */;
