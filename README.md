# What is this?

AssetTrack is a tax accounting program for calculating capital gains on
physical and virtual assets such as bullion and cryptocurrency investments. 
It can handle many aspects of physical investing that normal accounting
programs, such as Quicken and Microsoft Money, fail badly at:

- Preserving lot identity during transfers between vaults, which may appear
  as a separate buy and sell transaction 
- Tracking the basis adjustment due to asset storage costs and other
  investing costs
- Preserving lot identity across "like-kind" exchanges - such as, for
  example, selling silver in one service and immediately buying it via another.
- Treating the buy-sell spread on a "like-kind" transaction as a basis adjustment (cost to move
  between services or vaults)
- Transferring cryptocurrency between services (while retaining the original lot open date)
- Accounting for asset storage costs taken either by charging currency or by removing some of
  the asset from the holding
- Understanding that a metal asset retains its identity when addressed in different accounting 
  units (ie. automatic conversion between troy oz and grams)

It produces a result file of capital gains transactions and also outputs a
view of the current lots.
