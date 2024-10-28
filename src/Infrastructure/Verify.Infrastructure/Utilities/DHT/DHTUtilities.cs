using System.Collections.Concurrent;
using System.Numerics;
using Verify.Application.Dtos.Bank;

namespace Verify.Infrastructure.Utilities.DHT;


public static class DhtUtilities
{
    

    // Calculate XOR distance between two node IDs

    private static BigInteger XorDistance(byte[] hash1, byte[] hash2)
    {
        var xor = new byte[hash1.Length];
        for (int i = 0; i < hash1.Length; i++)
        {
            xor[i] = (byte)(hash1[i] ^ hash2[i]);
        }
        return new BigInteger(xor);
    }

    public static int CalculateXorDistance(byte[] param1, byte[] param2)
    {
        var xorResult = param1.Zip(param2, (b1, b2) => (byte)(b1 ^ b2)).ToArray();

        for (int i = 0; i < xorResult.Length; i++)
        {
            if (xorResult[i] != 0)
            {
                return (i * 8) + (int)Math.Log2(xorResult[i]);
            }
        }
        return 0; // They are identical
    }

    //public static int CalculateXorDistance(byte[] param1, byte[] param2)
    //{
    //    byte[] xorResult = new byte[param1.Length];
    //    for (int i = 0; i < param1.Length; i++)
    //    {
    //        xorResult[i] = (byte)(param1[i] ^ param2[i]);
    //    }

    //    // Find the leading bit where the IDs differ
    //    for (int i = 0; i < xorResult.Length; i++)
    //    {
    //        if (xorResult[i] != 0)
    //        {
    //            return (i * 8) + (int)Math.Log2(xorResult[i]);
    //        }
    //    }

    //    return 0; // They are identical
    //}

    //public static BigInteger CalculateXorDistance(byte[] hash1, byte[] hash2)
    //{
    //    // XOR the two byte arrays
    //    byte[] xorResult = new byte[hash1.Length];
    //    for (int i = 0; i < hash1.Length; i++)
    //    {
    //        xorResult[i] = (byte)(hash1[i] ^ hash2[i]);
    //    }

    //    // Convert the XOR result to a BigInteger for easy comparison
    //    return new BigInteger(xorResult);
    //}


    // Get bucket ID from XOR distance
    public static int GetBucketIdFromXorDistance(BigInteger xorDistance)
    {
        // Convert XOR distance to a byte array
        byte[] distanceBytes = xorDistance.ToByteArray();

        // Start counting the leading zeros
        int leadingZeros = 0;

        // Go through each byte and check for leading zeros
        foreach (byte b in distanceBytes)
        {
            if (b == 0)
            {
                // If the byte is 0, it contributes 8 leading zeros
                leadingZeros += 8;
            }
            else
            {
                // If the byte is not 0, count the leading zeros in this byte
                leadingZeros += CountLeadingZerosInByte(b);
                break; // Stop once we find the first non-zero byte
            }
        }

        return leadingZeros;
    }

    // XOR Distance Calculation
    public static int CalculateBucketIndex(byte[] myNodeId, byte[] newNodeId)
    {
        // XOR the node IDs to get the distance
        byte[] xorDistance = new byte[myNodeId.Length];
        for (int i = 0; i < myNodeId.Length; i++)
        {
            xorDistance[i] = (byte)(myNodeId[i] ^ newNodeId[i]);
        }

        // The index of the first 1 bit from the left gives us the bucket index
        // Convert XOR distance to an integer and get the most significant set bit position
        int bucketIndex = GetMostSignificantBitPosition(xorDistance);

        return bucketIndex;
    }

    // Helper function to get the index of the most significant bit set to 1
    private static int GetMostSignificantBitPosition(byte[] xorDistance)
    {
        for (int i = 0; i < xorDistance.Length; i++)
        {
            if (xorDistance[i] != 0)
            {
                return (i * 8) + (7 - BitOperations.LeadingZeroCount(xorDistance[i]));
            }
        }
        return -1;  // Nodes are identical if no bit is set
    }

    // Helper method to count leading zeros in a single byte
    public static int CountLeadingZerosInByte(byte value)
    {
        int count = 0;

        // Check each bit in the byte (from left to right)
        for (int i = 7; i >= 0; i--)
        {
            if ((value & (1 << i)) == 0)
            {
                count++;
            }
            else
            {
                break; // Stop counting once we hit the first 1 bit
            }
        }

        return count;
    }

    // Helper method for eviction policy (customizable)
    public static bool ShouldReplaceNode(NodeInfo leastRecentlySeenNode, NodeInfo newNode)
    {
        // Customize your eviction policy here, e.g., based on age, frequency, or node metrics
        // For example, we'll replace if the new node has been seen more recently than the least recently seen node
        return leastRecentlySeenNode.LastSeen < newNode.LastSeen;
    }

    // Helper method to queue rejected nodes (optional)
    public static void QueueRejectedNodeForFuture(NodeInfo rejectedNode)
    {
        // Implement a queue to reattempt adding the rejected node later
        // This could be a Redis queue, a memory cache, or a database table for retries
        // For demonstration, we will simulate a queue using a ConcurrentQueue
        var rejectedNodesQueue = new ConcurrentQueue<NodeInfo>();
        rejectedNodesQueue.Enqueue(rejectedNode);
    }
}

