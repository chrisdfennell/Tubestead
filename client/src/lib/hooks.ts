import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { api, type AppStatus, type LoginPayload, type SetupPayload } from './api'

const STATUS_KEY = ['app-status']

/** The single source of truth for "where are we": setup state, branding, user. */
export function useAppStatus() {
  return useQuery({
    queryKey: STATUS_KEY,
    queryFn: api.status,
    staleTime: 30_000,
    retry: false,
  })
}

export function useLogin() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (payload: LoginPayload) => api.login(payload),
    onSuccess: (user) =>
      qc.setQueryData<AppStatus>(STATUS_KEY, (prev) =>
        prev ? { ...prev, user } : prev,
      ),
  })
}

export function useLogout() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: () => api.logout(),
    onSuccess: () =>
      qc.setQueryData<AppStatus>(STATUS_KEY, (prev) =>
        prev ? { ...prev, user: null } : prev,
      ),
  })
}

export function useCompleteSetup() {
  const qc = useQueryClient()
  return useMutation({
    mutationFn: (payload: SetupPayload) => api.completeSetup(payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: STATUS_KEY }),
  })
}
