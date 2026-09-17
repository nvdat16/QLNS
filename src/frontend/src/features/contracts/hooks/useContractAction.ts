import { useCallback, useState } from "react";
import { ApiProblem } from "../../../api/apiClient";
import {
  performContractAction,
  type Contract,
  type ContractActionInput,
  type ContractLifecycleAction,
} from "../api/contractsApi";

export function useContractAction(onUpdated: (contract: Contract) => void) {
  const [busyContractId, setBusyContractId] = useState<number>();
  const [error, setError] = useState<string>();

  const run = useCallback(
    async (contract: Contract, action: ContractLifecycleAction, input: ContractActionInput = {}) => {
      setBusyContractId(contract.id);
      setError(undefined);
      try {
        onUpdated(await performContractAction(contract, action, input));
        return true;
      } catch (cause) {
        setError(
          cause instanceof ApiProblem
            ? cause.message
            : cause instanceof Error
              ? cause.message
              : "Không thể cập nhật trạng thái hợp đồng.",
        );
        return false;
      } finally {
        setBusyContractId(undefined);
      }
    },
    [onUpdated],
  );

  return { run, busyContractId, error };
}
