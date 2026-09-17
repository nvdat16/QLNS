import { useCallback, useEffect, useState } from "react";
import { listOffers, type Offer, type OfferPage } from "../api/offersApi";

export function useOffers() {
  const [data, setData] = useState<OfferPage>();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string>();

  const load = useCallback(async () => {
    setLoading(true);
    setError(undefined);
    try {
      setData(await listOffers({ pageSize: 20 }));
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Không thể tải danh sách thư mời nhận việc.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  const upsert = useCallback((offer: Offer) => {
    setData((prev) =>
      prev
        ? prev.items.some((item) => item.id === offer.id)
          ? { ...prev, items: prev.items.map((item) => (item.id === offer.id ? offer : item)) }
          : { ...prev, items: [offer, ...prev.items] }
        : prev,
    );
  }, []);

  return { data, loading, error, reload: load, upsert };
}
