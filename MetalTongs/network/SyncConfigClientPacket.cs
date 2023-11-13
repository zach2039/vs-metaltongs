using ProtoBuf;

namespace metaltongs.network
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class SyncConfigClientPacket
    {
        public bool TongsUsageConsumesDurability;
    }
}