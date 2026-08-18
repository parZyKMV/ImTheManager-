using UnityEngine;

/// <summary>
/// Datos de un arquetipo de cliente (Normal, Karen, Desastroso, Solo mirando, etc).
/// Los comportamientos reutilizables (ComplaintBehavior, MessBehavior) leen
/// estos valores para decidir si/como actuar - la diferencia entre arquetipos
/// esta 100% en los datos, no en codigo distinto por tipo de cliente.
/// </summary>
[CreateAssetMenu(fileName = "NewCustomerProfile", menuName = "I'm The Manager/Customer Profile")]
public class CustomerProfile : ScriptableObject
{
    [Header("Identidad")]
    public string profileName = "Cliente Normal";

    [Header("Compra")]
    [Tooltip("Si es false, el cliente solo mira (arquetipo 'Solo mirando'): nunca compra ni hace fila.")]
    public bool willBuy = true;
    [Tooltip("Probabilidad de explorar un wander point (zona sin productos) ANTES de ir a comprar. 0 = siempre va directo al estante.")]
    [Range(0f, 1f)] public float exploreFirstChance = 0f;

    [Header("Quejas (ComplaintBehavior)")]
    [Tooltip("Probabilidad de quejarse cuando se dispara alguno de los triggers de abajo.")]
    [Range(0f, 1f)] public float complaintChance = 0.1f;
    [Tooltip("Que situaciones pueden hacer que este cliente se queje.")]
    public ComplaintTrigger[] complaintTriggers = { ComplaintTrigger.EmptyShelf };
    [Tooltip("Cuanto estres le agrega al SanityMeter cada queja.")]
    [Min(0f)] public float complaintStressAmount = 10f;

    [Header("Desorden (CreateMessAction)")]
    [Tooltip("Probabilidad de dejar algun tipo de desorden. 0 = nunca (la mayoria de arquetipos).")]
    [Range(0f, 1f)] public float messChance = 0f;
    [Tooltip("Que tipos de desorden puede generar este arquetipo. Se elige uno al azar de esta lista.")]
    public MessType[] possibleMessTypes = { MessType.Trash };

    [Header("Preguntas tontas")]
    [Tooltip("Probabilidad de acercarse a hacer una pregunta random (sin impacto en Sanity). Ej: Cliente Curioso.")]
    [Range(0f, 1f)] public float askQuestionChance = 0f;

    [Header("Dialogo (opcional)")]
    [Tooltip("Nodo de Yarn a usar si este perfil dispara dialogo (ej. Karen -> Karen_Encounter, Curioso -> DumbQuestion_Encounter).")]
    public string dialogueStartNode;

    [Header("Destino alternativo (opcional, Fase 2)")]
    [Tooltip("Ej. 'Bathroom' para el cliente que busca el bano. Vacio = comportamiento normal.")]
    public string targetOverrideTag;
}

public enum ComplaintTrigger
{
    EmptyShelf,
    HighPrice,
    WrongChange,
    DirtyFloor
}

public enum MessType
{
    ShelfDisorder,    // deja el estante desordenado (rotado/item incorrecto)
    Trash,            // tira basura en el piso
    MisplacedProduct  // agarra un producto y lo deja en otro lado
}