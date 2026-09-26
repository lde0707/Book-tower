"""Regression checks for shell slivers and intentional puzzle openings."""
import unittest
import level_layout as level


class PaperGeometryTests(unittest.TestCase):
    def test_thin_ceiling_strips_are_not_discarded(self):
        holes = [dict(x0=-.98, x1=.98, z0=-.98, z1=.98)]
        pieces = level.ceiling_pieces(holes, 1, 1)
        self.assertAlmostEqual(sum((x1-x0)*(z1-z0) for x0,x1,z0,z1 in pieces),
                               4 - 1.96**2)

    def test_real_ceiling_covers_every_non_skylight_cell(self):
        for room in level.AREAS:
            hx, hz = room['size']['x']/2, room['size']['z']/2
            holes = room['skylights']
            pieces = level.ceiling_pieces(holes, hx, hz)
            xs = sorted({-hx, hx} | {v for h in holes for v in (h['x0'], h['x1'])})
            zs = sorted({-hz, hz} | {v for h in holes for v in (h['z0'], h['z1'])})
            for x0,x1 in zip(xs,xs[1:]):
                for z0,z1 in zip(zs,zs[1:]):
                    x,z = (x0+x1)/2,(z0+z1)/2
                    hole = any(h['x0'] < x < h['x1'] and h['z0'] < z < h['z1'] for h in holes)
                    covers = sum(a < x < b and c < z < d for a,b,c,d in pieces)
                    self.assertEqual(covers, 0 if hole else 1, (room['id'], x, z))

    def test_puzzle_gap_keeps_its_solve_bridge(self):
        room = next(a for a in level.AREAS if a['id'] == '1B')
        self.assertGreater(room['splitGap'], 0)
        self.assertTrue(any(o.get('bridgeFor') == room['puzzle']['id'] for o in room['objects']))

    def test_1d_south_wall_keeps_corner_sliver(self):
        room = next(a for a in level.AREAS if a['id'] == '1D')
        walls = [o for o in room['objects'] if o['name'].startswith('외벽_south_')]
        sliver = next(o for o in walls if 0 < o['scale']['x'] < .4)
        edge = sliver['pos']['x'] + sliver['scale']['x']/2
        self.assertAlmostEqual(edge, room['size']['x']/2, places=2)


if __name__ == '__main__':
    unittest.main()
