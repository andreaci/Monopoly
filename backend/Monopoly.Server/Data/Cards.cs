using Monopoly.Server.Domain;

namespace Monopoly.Server.Data;

// Classic US effects (post-2008 money amounts), with original concise bilingual captions.
// Square references use IDs so every language profile executes identical rules.
public static class Cards
{
    public static readonly Card[] All =
    [
        new("ch01","chance","Visit Boardwalk.","Vai a Parco della Vittoria.","move",Target:39),
        new("ch02","chance","Return to GO and collect the GO payment.","Vai al VIA e riscuoti il premio.","move",Target:0),
        new("ch03","chance","Visit Illinois Avenue; collect GO if you pass it.","Vai a Largo Colombo; riscuoti il premio se passi dal VIA.","move",Target:24),
        new("ch04","chance","Visit St. Charles Place; collect GO if you pass it.","Vai a Via Accademia; riscuoti il premio se passi dal VIA.","move",Target:11),
        new("ch05","chance","Go to the next railway. Pay double its usual rent if owned.","Vai alla prossima stazione. Se ha un proprietario, paga il doppio dell'affitto.","rail"),
        new("ch06","chance","Go to the next railway. Pay double its usual rent if owned.","Vai alla prossima stazione. Se ha un proprietario, paga il doppio dell'affitto.","rail"),
        new("ch07","chance","Go to the next utility. If owned, roll again and pay ten times the dice total.","Vai alla prossima società. Se ha un proprietario, tira ancora e paga dieci volte il totale.","utility"),
        new("ch08","chance","A dividend pays you {amount}.","Riscuoti un dividendo di {amount}.","receive",50),
        new("ch09","chance","Keep this card to leave jail without paying.","Conserva questa carta per uscire gratis di prigione.","jailCard"),
        new("ch10","chance","Move back three spaces.","Torna indietro di tre caselle.","back"),
        new("ch11","chance","Go straight to jail without collecting GO.","Vai direttamente in prigione senza riscuotere il VIA.","jail"),
        new("ch12","chance","Repair your properties: {amount} per house, {hotel} per hotel.","Ripara gli immobili: {amount} per casa, {hotel} per albergo.","repairs",25,HotelAmount:100),
        new("ch13","chance","Pay a speeding fine of {amount}.","Paga una multa di {amount}.","pay",15),
        new("ch14","chance","Travel to Reading Railroad; collect GO if you pass it.","Vai alla Stazione Sud; riscuoti il premio se passi dal VIA.","move",Target:5),
        new("ch15","chance","Pay each other player {amount}.","Paga {amount} a ogni altro giocatore.","payEach",50),
        new("ch16","chance","An investment matures. Receive {amount}.","Un investimento è scaduto. Riscuoti {amount}.","receive",150),
        new("cc01","chest","Return to GO and collect the GO payment.","Vai al VIA e riscuoti il premio.","move",Target:0),
        new("cc02","chest","A bank adjustment credits you {amount}.","Una rettifica bancaria ti accredita {amount}.","receive",200),
        new("cc03","chest","Pay your doctor {amount}.","Paga {amount} al medico.","pay",50),
        new("cc04","chest","A stock sale earns you {amount}.","La vendita di azioni ti frutta {amount}.","receive",50),
        new("cc05","chest","Keep this card to leave jail without paying.","Conserva questa carta per uscire gratis di prigione.","jailCard"),
        new("cc06","chest","Go straight to jail without collecting GO.","Vai direttamente in prigione senza riscuotere il VIA.","jail"),
        new("cc07","chest","Your holiday fund pays {amount}.","Il fondo vacanze ti versa {amount}.","receive",100),
        new("cc08","chest","Receive a tax refund of {amount}.","Ricevi un rimborso fiscale di {amount}.","receive",20),
        new("cc09","chest","For your birthday, collect {amount} from each other player.","Per il tuo compleanno ricevi {amount} da ogni altro giocatore.","collectEach",10),
        new("cc10","chest","Your insurance pays {amount}.","L'assicurazione ti versa {amount}.","receive",100),
        new("cc11","chest","Pay hospital costs of {amount}.","Paga {amount} di spese ospedaliere.","pay",100),
        new("cc12","chest","Pay tuition fees of {amount}.","Paga {amount} di tasse scolastiche.","pay",50),
        new("cc13","chest","Receive {amount} for consultancy work.","Riscuoti {amount} per una consulenza.","receive",25),
        new("cc14","chest","Street repairs cost {amount} per house and {hotel} per hotel.","Le riparazioni stradali costano {amount} per casa e {hotel} per albergo.","repairs",40,HotelAmount:115),
        new("cc15","chest","A competition prize earns you {amount}.","Il premio di un concorso ti frutta {amount}.","receive",10),
        new("cc16","chest","Receive an inheritance of {amount}.","Ricevi un'eredità di {amount}.","receive",100)
    ];
}
